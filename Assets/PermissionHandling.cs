using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class PermissionHandling : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("NOAHDEBUG Checking for permissions");
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            // The user authorized use of the microphone.
        }
        else
        {
            // We do not have permission to use the microphone.
            // Ask for permission or proceed without the functionality enabled.
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
