using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace boxClasses
{
    public class BoxManager
    {
        public List<Box> Boxes { get; private set; }
        private int RGBThreshold = 50;

        public BoxManager()
        {
            Boxes = new List<Box>();
        }

        public Box CreateBox(Transform parentTransform, int xc, float[] rgbValue)
        {
            Box newBox = new Box(parentTransform, xc, rgbValue);
            Boxes.Add(newBox);
            return newBox;
        }

        public void RemoveBoxesByColor(float[] rgbValue)
        {
            // Finding all boxes that match the RGB value
            var boxesToRemove = Boxes.Where(box => 
                Math.Abs(box.RgbValue[0] - rgbValue[0]) <= RGBThreshold &&
                Math.Abs(box.RgbValue[1] - rgbValue[1]) <= RGBThreshold &&
                Math.Abs(box.RgbValue[2] - rgbValue[2]) <= RGBThreshold
            ).ToList();
            foreach (var box in boxesToRemove)
            {
                if (box.GameObject != null)
                {
                    GameObject.Destroy(box.GameObject); // Properly destroy the GameObject
                }
                Boxes.Remove(box); // Remove the box from the list
            }
        }

        public void UseBoxFunctionalityByRGB(string message)
        {
            rgbValue = new float[3] {float.Parse(message.Split(' ')[6]), float.Parse(message.Split(' ')[7]), float.Parse(message.Split(' ')[8])};
            var boxesToUpdate = Boxes.Where(box => 
                Math.Abs(box.RgbValue[0] - rgbValue[0]) <= RGBThreshold &&
                Math.Abs(box.RgbValue[1] - rgbValue[1]) <= RGBThreshold &&
                Math.Abs(box.RgbValue[2] - rgbValue[2]) <= RGBThreshold
            ).ToList();
            foreach (var box in Boxes)
            {
                    box.messageToPoints(message);
            }
        }        

        public class Box
        {
            public GameObject GameObject { get; private set; }
            public int Xc1 { get; set; }
            public int Xc2 { get; set; }
            public float[] RgbValue { get; set; }
            public Vector3 Position {get; set;}
            public Vector3 tempPoint1, tempPoint2;
            Vector3 plane1P1 = new Vector3(3.3f, 0, 3.25f);
            Vector3 plane1P2 = new Vector3(-1.65f, 0, -1.35f);
            Vector3 plane2P1 = new Vector3(-1.65f, 0, 3.25f);
            Vector3 plane2P2 = new Vector3(3.3f, 0, -1.35f); 
            Vector3 camera1pos = new Vector3(-1.65f, 0, 3.25f);
            Vector3 camera2pos = new Vector3(-1.65f, 0, -1.35f);
            public Vector3 IntersectionPoint { get; set; }
            private const float minX = -1.65f, maxX = 3.3f;
            private const float minY = -1.35f, maxY = 3.25f;


            public Box(Transform parentTransform, int xc1, int xc2, float[] rgbValue)
            {
                Xc1 = xc1;
                Xc2 = xc2;
                RgbValue = rgbValue;
                GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject.transform.position = new Vector3(0, 0.75f, 0);
                GameObject.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
                GameObject.transform.parent = parentTransform;
                UpdateColor();
            }

            private void UpdateColor()
            {
                var renderer = GameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(RgbValue[0], RgbValue[1], RgbValue[2]);
                }
            }

            public void messageToPoints(string message)
            {
                int index = int.Parse(message.Slipt(' ')[1]);
                RgbValue = new float[3] {float.Parse(message.Split(' ')[6]), float.Parse(message.Split(' ')[7]), float.Parse(message.Split(' ')[8])};
                UpdateColor();
                switch(index)
                {
                    case 1:
                        Xc1 = int.Parse(message.Split(' ')[3]);
                        tempPoint1 = findPlanePoint(Xc1, plane1P1, plane1P2);
                    case 2:
                        Xc2 = int.Parse(message.Split(' ')[3]);
                        tempPoint2 = findPlanePoint(Xc2, plane2P1, plane2P2);
                }
                if(Xc1 != 0 && Xc2 != 0)
                {
                    IntersectionPoint = findIntersection(tempPoint1, camera1pos, tempPoint2, camera2pos);
                    Position = IntersectionPoint;
                    GameObject.transform.position = Position;
                }
            }
            private Vector3 findPlanePoint(int xc, Vector3 planePoint1, Vector3 planePoint2)
            {
                float a = (float)xc;
                float b = 512.0f - (float)xc;

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
        }
    }
}
