using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    public List<TargetPlatform> platforms;
    public Door door;

    private bool doorOpened = false;

    private void Start()
    {
        foreach (var platform in platforms)
        {
            platform.SetRoomManager(this);
        }
    }

    public void CheckPlatforms()
    {
        if (doorOpened)
            return; // A porta já está aberta

        foreach (var platform in platforms)
        {
            if (!platform.IsActivated)
            {
                return; // Nem todas as plataformas estão ativadas
            }
        }

        // Todas as plataformas estão ativadas, abrir a porta
        door.OpenDoor();
        doorOpened = true;
    }

    public void ResetRoom()
    {
        door.CloseDoor();
        doorOpened = false;

        // Resetar as plataformas
        foreach (var platform in platforms)
        {
            platform.ResetPlatform();
        }
    }
}
