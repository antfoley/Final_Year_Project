using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PimDeWitte.UnityMainThreadDispatcher;

public class newStickFigureScript : MonoBehaviour
{
    private string latestMessage1 = "";
    private string latestMessage2 = "";
    private Vector3 intersectionPoint = Vector3.zero;

    private UdpClient udpListener1;
    private UdpClient udpListener2;
    private bool isServerRunning = false;
    private GameObject box;
    private int counter;
    private int totalCounter;
    private float timeElapsed;
    private float totalElapsedTime;
    private const float minX = -1.65f, maxX = 3.3f;
    private const float minY = -1.35f, maxY = 3.25f;
    // private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    private void Start()
    {
        udpListener1 = new UdpClient(12345);
        udpListener2 = new UdpClient(12346);
        isServerRunning = true;
        Debug.Log("UDP Server is running");
        SpawnBox();
        StartListeningForMessages();
    }

    private void OnDestroy()
    {
        isServerRunning = false;
        udpListener1?.Close();
        udpListener2?.Close();
    }

    private void StartListeningForMessages()
    {
        Task.Run(async () => await ProcessMessageAsync(udpListener1, 1));
        Task.Run(async () => await ProcessMessageAsync(udpListener2, 2));
    }

    private async Task ProcessMessageAsync(UdpClient udpClient, int sourceId)
    {
        while (isServerRunning)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0/* 12344 + sourceId */);
            UdpReceiveResult receiveResult = await udpClient.ReceiveAsync();
            // byte[] bytesReceived = receiveResult.Buffer;
            string message = Encoding.ASCII.GetString(receiveResult.Buffer);
            // Debug.Log($"Received message from source {sourceId}: {message}");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                /* if (sourceId == 1)
                    latestMessage1 = message;
                else
                    latestMessage2 = message; */
                switch(sourceId)
                {
                    case 1: latestMessage1 = message; break;
                    case 2: latestMessage2 = message; break;
                    default: break;
                }
            });
        }
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        totalElapsedTime += Time.deltaTime;
        if (!string.IsNullOrEmpty(latestMessage1) && !string.IsNullOrEmpty(latestMessage2))
        {
            // Debug.Log("Calculating intersection point");
            // Vector3 p1, p2, p3, p4;
            var (p1, p3) = ConvertMessageToPoint(latestMessage1);
            var (p2, p4) = ConvertMessageToPoint(latestMessage2);
            // var intersection = Vector3.zero;
            Task.Run( async() =>
            {
                var intersection =  await CalculateIntersectionAsync(p1, p2, p3, p4);
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    intersectionPoint = intersection;
                    ApplyIntersectionPoint();
                });
                // try
                // {
                //     intersection = await CalculateIntersectionAsync(p1, p2, p3, p4);
                //     // Ensure thread-safe operations with Unity objects here
                //     Debug.Log("Task executed successfully.");
                //     intersectionPoint = intersection;
                    
                // }
                // catch (Exception ex)
                // {
                //     Debug.LogError($"Exception in task: {ex}");
                // }
            });
            // ApplyIntersectionPoint();
            latestMessage1 = "";
            latestMessage2 = "";
        }
        if (timeElapsed >= 1.0f)
        {
            Debug.Log($"Amount of movements in last second: {counter}");
            Debug.Log($"Average movement per second: {(float)totalCounter / totalElapsedTime}");
            timeElapsed = 0.0f;
            counter = 0;
        }
    }

    private async Task<Vector3> CalculateIntersectionAsync(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        return findIntersection(p1, p2, p3, p4);
    }

    private void ApplyIntersectionPoint()
    {
        if (float.IsNaN(intersectionPoint.x) || float.IsNaN(intersectionPoint.z))
        {
            return;
        }
        box.transform.position = intersectionPoint;
        counter++;
        totalCounter++;
        // Debug.Log($"Intersection point: {intersectionPoint} /n box moved!!!");
    }

    private (Vector3, Vector3) ConvertMessageToPoint(string message)
    {
        // Convert message to Vector3 points
        // Implement your own logic here
        Vector3 a, b;
        float xc = 0.0f;
        int index = 0;
        string[] messageParts = message.Split(' ');
        xc = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
        index = int.Parse(messageParts[5]);
        switch(index)
        {
            case 1: 
                a = findPlanePoint((int)xc, new Vector3(3.3f, 0, 3.25f), new Vector3(-1.65f, 0, -1.35f));
                b = new Vector3(-1.65f, 0, 3.25f);
                break;
            case 2:
                a = findPlanePoint((int)xc, new Vector3(-1.65f, 0, 3.25f), new Vector3(3.3f, 0, -1.35f));
                b = new Vector3(-1.65f, 0, -1.35f);
                break;
            default: 
                a = new Vector3(float.NaN, float.NaN, float.NaN);
                b = new Vector3(float.NaN, float.NaN, float.NaN);
                break;
        }
        return (a, b);
    }

    private Vector3 findPlanePoint(int xc, Vector3 planePoint1, Vector3 planePoint2)
    {
        float a = (float)xc;
        float b = 409.6f - (float)xc;

        Vector3 pointA = planePoint1;
        Vector3 pointB = planePoint2;

        Vector3 point = new Vector3(0, 0, 0);

        point.x = (pointA.x * b + pointB.x * a) / (a + b);
        point.z = (pointA.z * b + pointB.z * a) / (a + b);

        return point;
    }

    private Vector3 findIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float m1 = (p3.z - p1.z) / (p3.x - p1.x);
        float m2 = (p4.z - p2.z) / (p4.x - p2.x);
        
        float b1 = p1.z - m1 * p1.x;
        float b2 = p2.z - m2 * p2.x;
        
        if (Mathf.Abs(m1 - m2) < 0.0000001f)
        {
            return new Vector3(float.NaN, 0, float.NaN);
        }
        
        float x = (b2 - b1) / (m1 - m2);
        float z = m1 * x + b1;

        if(x <= minX){
            x = minX;
        }
        if(x >= maxX){
            x = maxX;
        }
        if(z <= minY){
            z = minY;
        }
        if(z >= maxY){
            z = maxY;
        }
        
        return new Vector3(x, 0, z);
    }

    void SpawnBox()
    {
        box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.position = new Vector3(0, 0.75f, 0);
        box.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        box.transform.parent = this.transform;
        box.GetComponent<Renderer>().material.color = Color.red;
    }
}