using Boomlagoon.JSON;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

public class LoadSongInfos : MonoBehaviour
{
    public List<Song> AllSongs = new List<Song>();
    public int CurrentSong
    {
        get
        {
            return Songsettings.CurrentSongIndex;
        }
        set
        {
            Songsettings.CurrentSongIndex = value;
        }
    }

    public RawImage Cover;
    public Text SongName;
    public Text Artist;
    public Text BPM;
    public Text Levels;
    private SongSettings Songsettings;
    private PermissionHandling PermissionHandling;

    private void Awake()
    {
        // Permissions--Only needed for Quest / Android!!!
        PermissionHandling = GameObject.FindGameObjectWithTag("PermissionHandling").GetComponent<PermissionHandling>();
        Songsettings = GameObject.FindGameObjectWithTag("SongSettings").GetComponent<SongSettings>();
    }

    private void OnEnable()
    {

        // this is for files which you put in the data folder by hand. On my quest that folder is:
        // /storage/emulated/0/Android/data/com.quest.opensabervr/files/Playlists
        // for historical reasons, the folder is still called "Playlists" even though 
        // BeatOn now calls the folder CustomSongs, and there's a separate playlist folder that does something else. 
        string internalPath = Path.Combine(Application.persistentDataPath + "/Playlists");
        string beatOnDataPath = Path.Combine("/sdcard/BeatOnData" + "/CustomSongsBREAK");
        LoadSongs(internalPath);
        LoadSongs(beatOnDataPath);
        // on Android, Unity compiles StreamingAssets into the base apk file, which is a jar. Basically a zip file.
        // we can still get files from it, but we can't do a directory list like we do with the other folders. 
        // in the interest of having at least one song always be a part of the build, I'm essentially hard-coding this one in.
        LoadSong(Path.Combine(Application.streamingAssetsPath + "/Playlists/hips_dont_lie/"));
    }

    private void LoadSongs(string path)
    {
        Debug.Log("NOADBEBUG persistentDataPath is " + path);
        if (Directory.Exists(path))
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (Directory.Exists(dir) && Directory.GetFiles(dir, "info.dat").Length > 0)
                {
                    LoadSong(dir);
                }
            }

            AllSongs = AllSongs.OrderBy(song => song.Name).ToList();
        }
    }

    private void LoadSong(string dir)
    {
        JSONObject infoFile = JSONObject.Parse(File.ReadAllText(Path.Combine(dir, "info.dat")));

        var song = new Song();
        song.Path = dir;
        song.Name = infoFile.GetString("_songName");
        song.AuthorName = infoFile.GetString("_songAuthorName");
        song.BPM = infoFile.GetNumber("_beatsPerMinute").ToString();
        song.CoverImagePath = Path.Combine(dir, infoFile.GetString("_coverImageFilename"));

        //a little logic to support Android file systems, hopefully without breaking support for fetching songs remotely
        song.AudioFilePath = Path.Combine(dir, infoFile.GetString("_songFilename"));
        var songPathProtocol = infoFile.GetString("_songFilename").Substring(0, 4).ToUpper();
        if (songPathProtocol != "HTTP" && songPathProtocol != "FILE")
        {
            song.AudioFilePath = "File://" + song.AudioFilePath;
        }

        song.PlayingMethods = new List<PlayingMethod>();
        Debug.Log("NOADBEBUG Parsed Song is " + song.Name);
        var difficultyBeatmapSets = infoFile.GetArray("_difficultyBeatmapSets");
        foreach (var beatmapSets in difficultyBeatmapSets)
        {
            PlayingMethod playingMethod = new PlayingMethod();
            playingMethod.CharacteristicName = beatmapSets.Obj.GetString("_beatmapCharacteristicName");
            playingMethod.Difficulties = new List<string>();

            foreach (var difficultyBeatmaps in beatmapSets.Obj.GetArray("_difficultyBeatmaps"))
            {
                playingMethod.Difficulties.Add(difficultyBeatmaps.Obj.GetString("_difficulty"));
            }

            song.PlayingMethods.Add(playingMethod);
        }
        Debug.Log("NOADBEBUG Adding Song is " + song.Name);
        AllSongs.Add(song);
    }

    public Song NextSong()
    {
        CurrentSong++;
        if(CurrentSong > AllSongs.Count - 1)
        {
            CurrentSong = 0;
        }

        Songsettings.CurrentSong = AllSongs[CurrentSong];

        return Songsettings.CurrentSong;
    }

    public Song PreviousSong()
    {
        CurrentSong--;
        if (CurrentSong < 0)
        {
            CurrentSong = AllSongs.Count - 1;
        }

        Songsettings.CurrentSong = AllSongs[CurrentSong];

        return Songsettings.CurrentSong;
    }

    public Song GetCurrentSong()
    {
        return Songsettings.CurrentSong;
    }
}

public class Song
{
    public string Path { get; set; }
    public string AudioFilePath { get; set; }
    public string Name { get; set; }
    public string AuthorName { get; set; }
    public string BPM { get; set; }
    public string CoverImagePath { get; set; }
    public List<PlayingMethod> PlayingMethods { get; set; }
    public int SelectedPlayingMethod { get; set; }
    public string SelectedDifficulty { get; set; }

    public string Hash
    {
        get
        {
            using (SHA1 hashGen = SHA1.Create())
            {
                var hash = hashGen.ComputeHash(Encoding.UTF8.GetBytes(Name + AuthorName + BPM));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}

public class PlayingMethod
{ 
    public string CharacteristicName { get; set; }
    public List<string> Difficulties { get; set; }
}
