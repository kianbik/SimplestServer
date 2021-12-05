using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;

    int reliableChannelID;

    int unreliableChannelID;

    int hostID;

    int socketPort = 5491;


    LinkedList<PlayerAccount> playerAccounts;

    const int PlayerAccountNameAndPassword = 1;


    string playerAccountsFilePath; 


    GameRoom






    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

        playerAccountsFilePath = = Application.dataPath + Path.DirectorySeparatorChar + "PartyAccounts.txt";
        playerAccounts = new LinkedList<PlayerAccount>();
        LoadPlayerAccounts();

        foreach(PlayerAccount pa in playerAccounts)
        {
            Debug.Log(name + " " + pa.password);
        }
        //Read in player account
    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }
  
    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }
    
    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');
        int signifier = int.Parse(csv[0]);

        if (signifier == clientToServerSignifier.CreateAccount)
        {
            //check if player account name is already exists
            string n = csv[1];
            string p = csv[2];
            bool nameIsInUse = false;

            foreach (PlayerAccount pa in playerAccounts)
            {
                if(pa.name == n)
                {
                    nameIsInUse = true;
                    break;
                }
            }

            if(nameIsInUse)
            {

                SendMessageToClient(ServerToClientSignifiers.AcountCreationFailed + "", id); 
           
            }
            else
            {
                PlayerAccount newPlayerAccount = new PlayerAccount(n, p);
                playerAccounts.AddLast(newPlayerAccount);
                SendMessageToClient(ServerToClientSignifiers.AcountCreationComplete + "", id);
                

                SavePlayerAccounts();
            }
        }
        else if (signifier == clientToServerSignifier.Login)
        {
            string n = csv[1];
            string p = csv[2];
            bool hasNameBeenFound = false;
            bool msgHasBeenSentToClient = false;
            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == n)
                {
                    hasNameBeenFound = true;
                    if (pa.password == p)
                    {
                        SendMessageToClient(ServerToClientSignifiers.LoginComplete + "", id);
                        msgHasBeenSentToClient = true;
                    }
                    else
                    {
                        SendMessageToClient(ServerToClientSignifiers.LoginFailed + "", id);
                        msgHasBeenSentToClient = true;
                    }
                }
              
            }
            if (!hasNameBeenFound)
            {
                if (!msgHasBeenSentToClient)
                {
                    SendMessageToClient(ServerToClientSignifiers.LoginFailed + "", id);
                }
            }
        }
        

    }
    private void SavePlayerAccounts()
    {
        StreamWriter sw = new StreamWriter(playerAccountsFilePath);

        foreach(PlayerAccount pa in playerAccounts)
        {
            sw.WriteLine(PlayerAccountNameAndPassword + "," + pa.name + "," + pa.password);
        }
        sw.Close();
    }
    private void LoadPlayerAccounts()
    {

        if (File.Exists(playerAccountsFilePath))
        {


            StreamReader sr = new StreamReader(playerAccountsFilePath);

            string line;

            while ((line = sr.ReadLine()) != null)
            {

                string[] csv = line.Split(',');

                int signifier = int.Parse(csv[0]);

                if (signifier == PlayerAccountNameAndPassword)
                {
                    PlayerAccount pa = new PlayerAccount(csv[1], csv[2]);
                    playerAccounts.AddLast(pa);

                }


            }

            sr.Close();
        }
    }


}

public class PlayerAccount
{
    public string name, password;

    public PlayerAccount(string Name, string Password)
    {
        name = Name;
        password = Password;
    }
}



public class GameRoom
{
    public PlayerAccount player1, player2;











}

public static class ClientToServerSignifiers
{

    public const int CreateAccount = 1;

    public const int Login = 2;





}

public static class ServerToClientSignifiers
{

    public const int LoginComplete = 1;

    public const int LoginFailed = 2;

    public const int AcountCreationComplete = 3;

    public const int AcountCreationFailed = 4;




}
