/* using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class newStickFigureScript : MonoBehaviour
{
    private  UdpClient udpListener1;
    private  UdpClient udpListener2;
    private bool isServerRunning = false;
    private const int Port1 = 12345;
    private const int Port2 = 12346;
    private IPEndPoint remoteEndPoint1;
    private IPEndPoint remoteEndPoint2;
    private string message1;
    private string message2;
    private string[] messageParts;
    private bool isUpdateNeeded = false;
    private float[] boxPoints = new float[3]; // mostLeft, mostRight, height
    private GameObject box;
    private float distance;
    Vector3 tempPosition1;
    Vector3 tempPosition2;
    Vector3 prevPosition;
    bool isTempPosition1Set = false;
    bool isTempPosition2Set = false;
    private Vector3 position;
    private Vector3 prevPosition1;
    private Vector3 prevPosition2;

    private readonly float A = (4.6f + (0.58f * Mathf.Sin(43.6f * Mathf.Deg2Rad))); //X-coordinates of the camera
    private readonly float B = -(0.58f * Mathf.Cos(43.6f * Mathf.Deg2Rad)); //Y-coordinates of the camera
    private float C;
    private const float minX = -1.65f, maxX = 3.3f;
    private const float minY = -1.35f, maxY = 3.25f;

    // Start is called before the first frame update
    void Start()
    {
        udpListener1 = new UdpClient(Port1);
        remoteEndPoint1 = new IPEndPoint(IPAddress.Any, Port1);
        udpListener2 = new UdpClient(Port2);
        remoteEndPoint2 = new IPEndPoint(IPAddress.Any, Port2);
        isServerRunning = true;
        Debug.Log("Server started...");
        SpawnBox();
    }

    // Update is called once per frame
    void Update()
    {
        ReceiveMessagesFromCam1();
        ReceiveMessagesFromCam2();
        if (isUpdateNeeded)
        {
            updatePart();
        }
        //box.transform.position = new Vector3(5.7f, 0, -4.0f);
    }

    // private async Task AcceptClientsAsync(TcpListener listener)
    // {
    //     while (isServerRunning)
    //     {
    //         TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
    //         HandleClientAsync(client);
    //     }
    // }

    // private async Task HandleClientAsync(TcpClient client)
    // {
    //     try
    //     {
    //         using (client)
    //         {
    //             NetworkStream stream = client.GetStream();
    //             byte[] buffer = new byte[1024];
    //             while (client.Connected)
    //             {
    //                 int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
    //                 if (bytesRead > 0)
    //                 {
    //                     string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
    //                     Debug.Log($"Received: {message}");
    //                     this.message = message;
    //                     //updatePart();
    //                     isUpdateNeeded = true;
    //                     // Example of sending a response back to the client
    //                     byte[] response = Encoding.ASCII.GetBytes("ACK");
    //                     await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
    //                 }
    //             }
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogError($"Client handling error: {e.Message}");
    //     }
    // }

    private void ReceiveMessagesFromCam1()
    {
        if (udpListener1.Available > 0)
        {
            byte[] bytesReceived = udpListener1.Receive(ref remoteEndPoint1);
            string message = Encoding.ASCII.GetString(bytesReceived);
            Debug.Log($"Received: {message}");
            this.message1 = message;
            isUpdateNeeded = true;
            byte[] bytesToSend = Encoding.ASCII.GetBytes("ACK");
            udpListener1.Send(bytesToSend, bytesToSend.Length, remoteEndPoint1);
        }
    }

    private void ReceiveMessagesFromCam2()
    {
        if (udpListener2.Available > 0)
        {
            byte[] bytesReceived = udpListener2.Receive(ref remoteEndPoint2);
            string message = Encoding.ASCII.GetString(bytesReceived);
            Debug.Log($"Received: {message}");
            this.message2 = message;
            isUpdateNeeded = true;
            byte[] bytesToSend = Encoding.ASCII.GetBytes("ACK");
            udpListener2.Send(bytesToSend, bytesToSend.Length, remoteEndPoint2);
        }
    }

    private void OnApplicationQuit()
    {
        isServerRunning = false;
        udpListener1.Close();
        udpListener2.Close();
    }

    void SpawnBox()
    {
        box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.position = new Vector3(0, 0.75f, 0);
        box.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
        box.transform.parent = this.transform;
        box.GetComponent<Renderer>().material.color = Color.red;
    }
    

    void updatePart()
    {
        // //private float boxPoints[] = new float[3]; // mostLeft, mostRight, height
        // messageParts = message.Split(' ');
        // //xc is the x-coordinate of the object in the image
        // float xc = 0.0f;
        // int index = 0;
        // //float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]) //x function, y is just change 0 to 1
        // // boxPoints[1] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
        // // boxPoints[0] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
        // // boxPoints[2] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[1]);
        // //message part 1 is string of body part
        // //message part 3 is the x and y coordinates of the body part
        // switch(messageParts[1])
        // {
        //     case "Neck":
        //         xc = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
        //         index = int.Parse(messageParts[5]);
        //         break;
        //     // case "R-Wr":
        //     //     boxPoints[0] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
        //     //     break;
        //     // case "L-Wr":
        //     //     boxPoints[1] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
        //     //     break;
        //     // case "L-Ank":
        //     //     //boxPoints[2] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[1]);
        //     //     boxPoints[2] = ((xc - float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0])) > 0) ? (xc - float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0])) : (float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]) );
        //     //     break;
        //     // case "R-Ank":
        //     //     //boxPoints[2] = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[1]);
        //     //     boxPoints[2] = ((xc - float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0])) > 0) ? (xc - float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0])) : (float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]) );
        //     //     break;
        // }
        // moveBox(xc, index);
        // Parse message1 and message2 data
        float xc = 0.0f;
        int index = 0;
        if (!string.IsNullOrEmpty(message1))
        {
            string[] messageParts = message1.Split(' ');
            if (messageParts.Length >= 6)
            {
                xc = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
                index = int.Parse(messageParts[5]);
            }
            message1 = "";
        }
        else if (!string.IsNullOrEmpty(message2))
        {
            string[] messageParts = message2.Split(' ');
            if (messageParts.Length >= 6)
            {
                xc = float.Parse(messageParts[3].Substring(1, messageParts[3].Length - 2).Split(',')[0]);
                index = int.Parse(messageParts[5]);
            }
            message2 = "";
        }

        // Call moveBox with parsed data
        moveBox(xc, index);
    }

    void moveBox(float xc, int index)
    {
        //d = (focalLength(mm) * realHight(mm, assumed to be 1.7m(average height in ireland)) * imageHeight(3040 pixels for oakd s2))/(boxPoints[2](object hieght) * SensorHeight(6.3mm for the sony IMX378)
        //default distance is 3m
        // distance = (((4.81f * 1500.0f * 3040.0f)/(boxPoints[2] * 6.3f + 0.000000000001f) > 10)) ? ((4.81f * 1500.0f * 3040.0f)/(boxPoints[2] * 6.3f+ 0.000000000001f)/10000.0f) : 3;
        //distance = 4.0f;
        // Debug.Log($"Distance: {distance}, BoxPoints[2]: {boxPoints[2]}");
        //for this x, y is used on the 2D plane for simplicity of maths 
        // float x = getX(xc);
        // float y = getY(xc, x);
        // Matrix4x4 translationMatrix = new Matrix4x4(
        //     1, 0, 0, -10.306f,
        //     0, 1, 0, 0,
        //     0, 0, 1, 3.2026f,
        //     0, 0, 0, 1
        // );
        // position = FindIntersectionsPlane1((int)xc);
        // Debug.Log($"x: {point.x}, y: {point.y}");
        // Vector3 point = new Vector3(x, 0, y);
        // box.transform.position = translationMatrix.TransformPoint(point); //x and y are the x and y coordinates of the object in the image
        // static Vector3 tempPosition1 = null;
        // static Vector3 tempPosition2 = null;
        // if(index==2){return;}
        // if(index == 1){    
            tempPosition1 = FindIntersectionsPlane1((int)xc);
            isTempPosition1Set = true;
        // }
        // else if(index == 2){
            tempPosition2 = FindIntersectionsPlane2((int)xc);
            isTempPosition2Set = true;
        // }
        // Vector3 tempPosition2 = FindIntersectionsPlane2((int)xc);
        if(!isTempPosition1Set || !isTempPosition2Set)
        {
            return;
        }
        position = findIntersection(tempPosition1, tempPosition2, new Vector3(-1.65f, 0, 3.25f), new Vector3(-1.65f, 0, -1.35f));
        //position = tempPosition1; //just for debuging cam 1
        isTempPosition1Set = false;
        isTempPosition2Set = false;
        prevPosition1 = tempPosition1;
        prevPosition2 = tempPosition2;
        box.transform.position = position;
        prevPosition = position;
        // Debug.Log($"Moving box to point{position}, xc: {xc}");
        // yield return new WaitForSeconds(2);
    }

//    Vector3 FindIntersectionsPlane1(int xc)
//    {
//        // List<Vector2> intersections = new List<Vector2>();
//        if(xc == 0)
//        {
//            return prevPosition1;
//        }
//        float radiusSquared = Mathf.Pow((6.7f * (410 - xc)) / 409.6f, 2);
//
//        // float A = 1 + Mathf.Pow(0.929f, 2);
//        // float B = 2 * (0.929f * 0.183f + 1.65f + 1.35f * 0.929f);
//        // float C = Mathf.Pow(1.65f, 2) + Mathf.Pow(0.183f, 2) + 2 * 1.35f * 0.183f + 4.8025f - radiusSquared;
//
//        //parameters for the quadratic equation for plane 1
//        const float lineSlope = 0.929f;
//        const float lineYIntercept = 0.183f;
//        const float circleCenterX = -1.65f;
//        const float circleCenterY = -1.35f;
//
//        float A = 1 + Mathf.Pow(lineSlope, 2);
//        float B = 2 * (lineSlope * lineYIntercept + circleCenterX - circleCenterY * lineSlope);
//        float C = Mathf.Pow(circleCenterX, 2) + Mathf.Pow(lineYIntercept, 2) + 2 * circleCenterY * lineYIntercept - radiusSquared;
//
//        float D = Mathf.Pow(B, 2) - 4 * A * C;
//
//        // Debug.Log($"A: {A}, B: {B}, C: {C}, D: {D}");
//
//        float x1 = 0.0f, x2 = 0.0f, y1 = 0.0f, y2 = 0.0f;
//
//        if (D >= 0)
//        {
//            x1 = (-B + Mathf.Sqrt((float)D)) / (2 * A);
//            x2 = (-B - Mathf.Sqrt((float)D)) / (2 * A);
//            y1 = 0.929f * x1 + 0.183f;
//            y2 = 0.929f * x2 + 0.183f;
//
//            // Debug.Log($"x1: {x1}, y1: {y1}, x2: {x2}, y2: {y2}");
//
//            if (x1 >= minX && x1 <= maxX && y1 >= minY && y1 <= maxY)
//            {
//                // intersections.Add(new Vector2((float)x1, (float)y1));
//                return new Vector3((float)x1, 0, (float)y1);
//            }
//            
//            if (D > 0 && x2 >= minX && x2 <= maxX && y2 >= minY && y2 <= maxY)
//            {
//                // intersections.Add(new Vector2((float)x2, (float)y2));
//                return new Vector3((float)x2, 0, (float)y2);
//            }
//        }
//
//        return new Vector3(0, 0, 0);
//    }

    Vector3 FindIntersectionsPlane1(int xc){
        float a = (float)xc;
        float b = 409.6f - (float)xc;

        Vector3 pointB = new Vector3(-1.65f, 0, -1.35f);
        Vector3 pointA = new Vector3(3.3f, 0, 3.25f);


        Vector3 point = new Vector3(0, 0, 0);
        if (a == 0 || b == 0)
        {
            return prevPosition1;
        }

        point.x = (pointA.x * b + pointB.x * a) / (a + b);
        point.z = (pointA.z * b + pointB.z * a) / (a + b);

        return point;
    }

    // Vector3 FindIntersectionsPlane2(int xc)
    // {
    //     // List<Vector2> intersections = new List<Vector2>();
    //     if(xc == 0)
    //     {
    //         return prevPosition2;
    //     }
    //     float radiusSquared = Mathf.Pow((6.7f * (xc)) / 409.6f, 2);

    //     // float A = 1 + Mathf.Pow(0.929f, 2);
    //     // float B = 2 * (0.929f * 0.183f + 1.65f + 1.35f * 0.929f);
    //     // float C = Mathf.Pow(1.65f, 2) + Mathf.Pow(0.183f, 2) + 2 * 1.35f * 0.183f + 4.8025f - radiusSquared;

    //     //parameters for the quadratic equation for plane 1
    //     // float A = 1.862641f;
    //     // float B = 6.150786f;
    //     // float C = 5.067589f - radiusSquared;

    //     const float lineSlope = -0.929f;
    //     const float lineYIntercept = 1.717f;
    //     const float circleCenterX = 3.3f;
    //     const float circleCenterY = -1.35f;

    //     float A = 1 + Mathf.Pow(lineSlope, 2);
    //     float B = 2 * (lineSlope * lineYIntercept + circleCenterX - circleCenterY * lineSlope);
    //     float C = Mathf.Pow(circleCenterX, 2) + Mathf.Pow(lineYIntercept, 2) + 2 * circleCenterY * lineYIntercept - radiusSquared;

    //     float D = Mathf.Pow(B, 2) - 4 * A * C;
    //     // D = Math.Abs(D);

    //     Debug.Log($"A: {A}, B: {B}, C: {C}, D: {D}");

    //     float x1 = 0.0f, x2 = 0.0f, y1 = 0.0f, y2 = 0.0f;

    //     if (D >= 0)
    //     {
    //         x1 = (-B + Mathf.Sqrt((float)D)) / (2 * A);
    //         x2 = (-B - Mathf.Sqrt((float)D)) / (2 * A);
    //         y1 = lineSlope * x1 + lineYIntercept;
    //         y2 = lineSlope * x2 + lineYIntercept;

    //         // Debug.Log($"x1: {x1}, y1: {y1}, x2: {x2}, y2: {y2}");

    //         if (x1 >= minX && x1 <= maxX && y1 >= minY && y1 <= maxY)
    //         {
    //             // intersections.Add(new Vector2((float)x1, (float)y1));
    //             return new Vector3((float)x1, 0, (float)y1);
    //         }
            
    //         if (D > 0 && x2 >= minX && x2 <= maxX && y2 >= minY && y2 <= maxY)
    //         {
    //             // intersections.Add(new Vector2((float)x2, (float)y2));
    //             return new Vector3((float)x2, 0, (float)y2);
    //         }
    //     }

    //     return new Vector3(0, 0, 0);
    // }

    Vector3 FindIntersectionsPlane2(int xc){
        float a = (float)xc;
        float b = 409.6f - (float)xc;

        Vector3 pointA = new Vector3(-1.65f, 0, 3.25f);
        Vector3 pointB = new Vector3(3.3f, 0, -1.35f);


        Vector3 point = new Vector3(0, 0, 0);
        if (a == 0 || b == 0)
        {
            return prevPosition2;
        }

        point.x = (pointA.x * b + pointB.x * a) / (a + b);
        point.z = (pointA.z * b + pointB.z * a) / (a + b);

        return point;
    }


    /**
    * 
    * @param xc x-coordinate of the object in the image
    * @return x-coordinate of the object in the real world
    */
    // float getX(float xc)
    // {
    //     float xr = (xc / 4096)*6.7f;
    //     C = (float)Math.Pow((((4.6/5)*xr - B) / (xr - A)),2);
    //     float a,b,c;
    //     a = 1.0f + C;
    //     b = -2*A - 2*C*A;
    //     c = (float)Math.Pow(A, 2) + (float)Math.Pow(A, 2)*C - (float)Math.Pow(distance, 2);
    //     //Debug.Log($"a: {a}, b: {b}, c: {c}, real: {b * b - 4 * a * c}");
    //     return ((-b + (float)Math.Sqrt(b * b - 4 * a * c)) / (2 * a) == (-b + (float)Math.Sqrt(b * b - 4 * a * c)) / (2 * a)) ? (-b + (float)Math.Sqrt(b * b - 4 * a * c)) / (2 * a) : 0;
    // }


    /**
    *
    * @param xc x-coordinate of the object in the image
    * @param x x-coordinate of the object in the real world
    * @param C constant
    * @return y-coordinate of the object in the real world (2D Plane)
    */
    // float getY(float xc, float x)
    // {
    //     // float xr = (xc / 4096)*6.7f;
    //     // float y = (float)Math.Sqrt(C)*(xr - A) + B;
    //     // return (y*100) - 100;
    //     float xr = (xc / 4096)*6.7f;
    //     C = (float)Math.Pow((((4.6/5)*xr - B) / (xr - A)),2);
    //     return (float)Math.Sqrt(C)*(x - A) + B;
    // }

    //p1, p3 same line
    //p2, p4 same line
    /*
    Vector3 findIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // float A1, B1, C1, A2, B2, C2;
        // A1 = - ((p3.z - p1.z)/(p3.x - p1.x));
        // B1 = 1;
        // C1 = -p1.z + (Mathf.Abs(A1))*p1.x;
        // A2 = - ((p4.z - p2.z)/(p4.x - p2.x));
        // B2 = 1;
        // C2 = -p2.z + (Mathf.Abs(A2))*p2.x;
        // float x = ((B1*C2)-(B2*C1))/((A1*B2)-(A2*B1));
        // float z = ((A2*C1)-(A1*C2))/((A1*B2)-(A2*B1));
        // if (x >= minX && x <= maxX && z >= minY && z <= maxY){
        //     if(x <= minX){
        //         x = minX;
        //     }
        //     if(x >= maxX){
        //         x = maxX;
        //     }
        //     if(z <= minY){
        //         z = minY;
        //     }
        //     if(z >= maxY){
        //         z = maxY;
        //     }
        // }
        // return new Vector3(x, 0, z);
        // Calculate the slopes (m1 and m2) of the two lines
        float m1 = (p3.z - p1.z) / (p3.x - p1.x);
        float m2 = (p4.z - p2.z) / (p4.x - p2.x);
        
        // Calculate the y-intercepts (b1 and b2) of the lines
        float b1 = p1.z - m1 * p1.x;
        float b2 = p2.z - m2 * p2.x;
        
        // Check if the lines are parallel (have the same slope)
        if (Mathf.Abs(m1 - m2) < 0.001f)
        {
            // Lines are parallel (or coincident), no intersection or infinite intersections
            // Handle accordingly; here we return an indicative value
            return new Vector3(float.NaN, 0, float.NaN);
        }
        
        // Calculate the intersection point
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


    public struct Matrix4x4
    {
        private float[,] matrix;

        public Matrix4x4(float m00, float m01, float m02, float m03,
                        float m10, float m11, float m12, float m13,
                        float m20, float m21, float m22, float m23,
                        float m30, float m31, float m32, float m33)
        {
            matrix = new float[4, 4] {
                { m00, m01, m02, m03 },
                { m10, m11, m12, m13 },
                { m20, m21, m22, m23 },
                { m30, m31, m32, m33 }
            };
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            float x = matrix[0, 0] * point.x + matrix[0, 1] * point.y + matrix[0, 2] * point.z + matrix[0, 3];
            float y = matrix[1, 0] * point.x + matrix[1, 1] * point.y + matrix[1, 2] * point.z + matrix[1, 3];
            float z = matrix[2, 0] * point.x + matrix[2, 1] * point.y + matrix[2, 2] * point.z + matrix[2, 3];
            return new Vector3(x, y, z);
        }
    }
} */