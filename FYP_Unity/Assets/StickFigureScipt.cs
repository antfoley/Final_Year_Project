using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class StickFigureCreator : MonoBehaviour
{
    public struct BodyPart
    {
        public string name;
        public Vector3 position;

        public BodyPart(string name, Vector3 position)
        {
            this.name = name;
            this.position = position;
        }
    }

    // Array of body parts with names and positions
    private BodyPart[] bodyParts = new BodyPart[]
    {
        new BodyPart("R-Sho",   new Vector3(0.0f, 1.5f, 0)),
        new BodyPart("R-Elb",   new Vector3(0.25f, 1.0f, 0)),
        new BodyPart("R-Wr",    new Vector3(0.5f, 0.75f, 0)),
        new BodyPart("L-Sho",   new Vector3(-0.0f, 1.5f, 0)),
        new BodyPart("L-Elb",   new Vector3(-0.25f, 1.0f, 0)),
        new BodyPart("L-Wr",    new Vector3(-0.5f, 0.75f, 0)),
        new BodyPart("R-Hip",   new Vector3(0.25f, -1.0f, 0)),
        new BodyPart("R-Knee",  new Vector3(0.25f, -1.25f, 0)),
        new BodyPart("R-Ank",   new Vector3(0.5f, -1.75f, 0)),
        new BodyPart("L-Hip",   new Vector3(-0.25f, -1.0f, 0)),
        new BodyPart("L-Knee",  new Vector3(-0.25f, -1.25f, 0)),
        new BodyPart("L-Ank",   new Vector3(-0.5f, -1.75f, 0))
    };

    private GameObject[] bodyPartsGameObjects = new GameObject[12];

    private TcpListener tcpListener;
    private bool isServerRunning = false;
    private const int Port = 12345;
    private string message;
    private string[] messageParts;
    private string[] jointNames = new string[] { "R-Sho", "R-Elb", "R-Wr", "L-Sho", "L-Elb", "L-Wr", "R-Hip", "R-Knee", "R-Ank", "L-Hip", "L-Knee", "L-Ank" };
    private string[] linkNames = new string[] { "R-Sho2R-Elb", "R-Elb2R-Wr", "L-Sho2L-Elb", "L-Elb2L-Wr", "R-Hip2R-Knee", "R-Knee2R-Ank", "L-Hip2L-Knee", "L-Knee2L-Ank" };
    int[] startingPoint = new int[2];
    // int[] endingPoint = new int[2];
    Vector3 newjointPosition = new Vector3(0, 0, 0);
    bool isUpdateNeeded = false;

    void Start()
    {
        tcpListener = new TcpListener(IPAddress.Any, Port);
        isServerRunning = true;
        tcpListener.Start();
        Debug.Log("Server started...");

        foreach (BodyPart part in bodyParts)
        {
            CreateBodyPart(part.name, part.position, transform);
        }
        createHeirachy();
        for (int i = 0; i < 8; i++)
        {
            CreateConnectingCylinder(linkNames[i], bodyPartsGameObjects[Array.IndexOf(jointNames, linkNames[i].Split('2')[0])], bodyPartsGameObjects[Array.IndexOf(jointNames, linkNames[i].Split('2')[1])]);
        }
    }

    void Update()
    {
        AcceptClientsAsync(tcpListener);
        if (isUpdateNeeded)
        {
            updateBodyPart();
        }
    }

    private async Task AcceptClientsAsync(TcpListener listener)
    {
        while (isServerRunning)
        {
            TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Debug.Log($"Received: {message}");
                        this.message = message;
                        // updateBodyPart();
                        isUpdateNeeded = true;
                        // Example of sending a response back to the client
                        byte[] response = Encoding.ASCII.GetBytes("ACK");
                        await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Client handling error: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        isServerRunning = false;
        tcpListener.Stop();
    }

    void CreateBodyPart(string name, Vector3 position, Transform parent)
    {
        GameObject part;
        if (name.Contains("Elb") || name.Contains("Wr") || name.Contains("Knee") || name.Contains("Ank"))
        {
            part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        else
        {
            part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }

        part.name = name;
        part.transform.position = position;
        part.transform.SetParent(parent, false);
        switch (part.GetComponent<Collider>())
        {
            case SphereCollider _:
                part.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                break;
            case CapsuleCollider _:
                part.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
                break;
        }

        // Save the created body part to the bodyParts array
        int index = Array.FindIndex(bodyParts, bodyPart => bodyPart.name == name);
        if (index != -1)
        {
            bodyPartsGameObjects[index] = part;
        }
    }

    void updateBodyPart()
    {
        messageParts = message.Split(' ');
        int index = Array.IndexOf(jointNames, messageParts[1]);
        newjointPosition.x = (float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0])) / 1000;
        newjointPosition.y = (float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[1])) / 1000;
        float angle = Mathf.Atan2(newjointPosition.y, newjointPosition.x);

        // Smoothly interpolate the position and rotation of the body part
        float lerpSpeed = 5f; // Adjust the speed of interpolation as needed
        Vector3 currentPosition = this.bodyPartsGameObjects[index].transform.position;
        Quaternion currentRotation = this.bodyPartsGameObjects[index].transform.rotation;
        Vector3 targetPosition = newjointPosition;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        float t = Time.deltaTime * lerpSpeed;
        this.bodyPartsGameObjects[index].transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, t);
        this.bodyPartsGameObjects[index].transform.localRotation = Quaternion.Lerp(currentRotation, targetRotation, t);

        isUpdateNeeded = false;
    }

    void createHeirachy()
    {
        // private BodyPart[] bodyParts = new BodyPart[]
        // {
        //     1 new BodyPart("R-Sho",   new Vector3(0.5f, 1.5f, 0)),
        //     2 new BodyPart("R-Elb",   new Vector3(1.0f, 1.0f, 0)),
        //     3 new BodyPart("R-Wr",    new Vector3(1.5f, 0.5f, 0)),
        //     4 new BodyPart("L-Sho",   new Vector3(-0.5f, 1.5f, 0)),
        //     5 new BodyPart("L-Elb",   new Vector3(-1.0f, 1.0f, 0)),
        //     7 new BodyPart("L-Wr",    new Vector3(-1.5f, 0.5f, 0)),
        //     8 new BodyPart("R-Hip",   new Vector3(0.25f, -1.0f, 0)),
        //     9 new BodyPart("R-Knee",  new Vector3(0.5f, -1.5f, 0)),
        //     10 new BodyPart("R-Ank",   new Vector3(0.75f, -2.0f, 0)),
        //     11 new BodyPart("L-Hip",   new Vector3(-0.25f, -1.0f, 0)),
        //     12 new BodyPart("L-Knee",  new Vector3(-0.5f, -1.5f, 0)),
        //     13 new BodyPart("L-Ank",   new Vector3(-0.75f, -2.0f, 0))
        // };


        // Set the parent of body part 0 to the main transform
        // shoulder rigth to transform
        bodyPartsGameObjects[0].transform.SetParent(transform, false);

        // Set the parent of body part 1 to body part 0
        // elbow right to shoulder right
        bodyPartsGameObjects[1].transform.SetParent(bodyPartsGameObjects[0].transform, false);

        // Set the parent of body part 2 to body part 1
        // wrist right to elbow right
        bodyPartsGameObjects[2].transform.SetParent(bodyPartsGameObjects[1].transform, false);

        // Set the parent of body part 3 to the main transform
        // shoulder left to transform
        bodyPartsGameObjects[3].transform.SetParent(transform, false);

        // Set the parent of body part 4 to body part 3
        // elbow left to shoulder left
        bodyPartsGameObjects[4].transform.SetParent(bodyPartsGameObjects[3].transform, false);

        // Set the parent of body part 5 to body part 4
        // wrist left to elbow left
        bodyPartsGameObjects[5].transform.SetParent(bodyPartsGameObjects[4].transform, false);

        // Set the parent of body part 6 to the main transform
        // hip right to transform
        bodyPartsGameObjects[6].transform.SetParent(transform, false);

        // Set the parent of body part 7 to body part 6
        // knee right to hip right
        bodyPartsGameObjects[7].transform.SetParent(bodyPartsGameObjects[6].transform, false);

        // Set the parent of body part 8 to body part 7
        // ankle right to knee right
        bodyPartsGameObjects[8].transform.SetParent(bodyPartsGameObjects[7].transform, false);

        // Set the parent of body part 9 to the main transform
        // hip left to transform
        bodyPartsGameObjects[9].transform.SetParent(transform, false);

        // Set the parent of body part 10 to body part 9
        // knee left to hip left
        bodyPartsGameObjects[10].transform.SetParent(bodyPartsGameObjects[9].transform, false);

        // Set the parent of body part 11 to body part 10
        // ankle left to knee left
        bodyPartsGameObjects[11].transform.SetParent(bodyPartsGameObjects[10].transform, false);
    }

    void CreateConnectingCylinder(string name, GameObject joint1, GameObject joint2)
    {
        Vector3 direction = joint2.transform.position - joint1.transform.position;
        float distance = direction.magnitude;
        Vector3 middlePoint = (joint1.transform.position + joint2.transform.position) / 2;

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.position = middlePoint;
        cylinder.transform.up = direction;
        cylinder.transform.localScale = new Vector3(0.05f, distance / 2, 0.05f); // Adjust the scale accordingly
        cylinder.transform.SetParent(joint1.transform, true); // Set parent to maintain hierarchy
    }

}
