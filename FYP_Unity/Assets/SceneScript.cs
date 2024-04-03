using UnityEngine;

public class RoomOutline : MonoBehaviour
{
    public Vector3 roomDimensions = new Vector3(11.7f, 3f, 8f); // Length, Height, Width
    public Vector3 tableDimensions = new Vector3(2f, 0.75f, 1f); // Length, Height, Width
    public Material wallMaterial;
    public Material floorMaterial;
    public Material tableMaterial;
    // public Material defaultMaterial;
    // public GameObject ragdollPrefab;
    // private GameObject ragdollInstance;
    // private float moveInterval = 2.0f; // Move every 2 seconds
    // private float timer;

    //run once at the start
    void Start()
    {
        CreateRoom();
        CreateTables();
        
    }
    
    void CreateRoom()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.localScale = new Vector3(roomDimensions.x, 0.1f, roomDimensions.z);
        floor.transform.position = new Vector3(0, -0.05f, 0);
        floor.GetComponent<Renderer>().material = floorMaterial;
        // floor.GetComponent<Renderer>().material.color = Color.grey;
        floor.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.5f);

        GameObject floorParent = new GameObject("Floor");  
        floor.transform.parent = floorParent.transform;

        GameObject wallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallFront.transform.localScale = new Vector3(roomDimensions.x, roomDimensions.y, 0.1f);
        wallFront.transform.position = new Vector3(0, roomDimensions.y / 2, roomDimensions.z / 2);
        wallFront.GetComponent<Renderer>().material = wallMaterial;
        wallFront.GetComponent<Renderer>().material.color = Color.clear;

        GameObject wallBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallBack.transform.localScale = new Vector3(roomDimensions.x, roomDimensions.y, 0.1f);
        wallBack.transform.position = new Vector3(0, roomDimensions.y / 2, -roomDimensions.z / 2);
        wallBack.GetComponent<Renderer>().material = wallMaterial;
        wallBack.GetComponent<Renderer>().material.color = Color.clear;

        GameObject wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallLeft.transform.localScale = new Vector3(0.1f, roomDimensions.y, roomDimensions.z);
        wallLeft.transform.position = new Vector3(-roomDimensions.x / 2, roomDimensions.y / 2, 0);
        wallLeft.GetComponent<Renderer>().material = wallMaterial;
        wallLeft.GetComponent<Renderer>().material.color = Color.clear;

        GameObject wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallRight.transform.localScale = new Vector3(0.1f, roomDimensions.y, roomDimensions.z);
        wallRight.transform.position = new Vector3(roomDimensions.x / 2, roomDimensions.y / 2, 0);
        wallRight.GetComponent<Renderer>().material = wallMaterial;
        wallRight.GetComponent<Renderer>().material.color = Color.clear;

        // Set parent for all walls (optional, for better hierarchy management)
        GameObject wallsParent = new GameObject("Walls");
        wallFront.transform.parent = wallsParent.transform;
        wallBack.transform.parent = wallsParent.transform;
        wallLeft.transform.parent = wallsParent.transform;
        wallRight.transform.parent = wallsParent.transform;
    }

    void CreateTables()
    {
        // Table along the front wall
        GameObject tableFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableFront.transform.localScale = new Vector3(roomDimensions.x, 1f, 0.75f);
        tableFront.transform.position = new Vector3(0, 0.5f, roomDimensions.z / 2 - 0.75f / 2);
        tableFront.GetComponent<Renderer>().material = tableMaterial;
        tableFront.GetComponent<Renderer>().material.color = Color.grey;

        // Table along the left wall
        GameObject tableLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableLeft.transform.localScale = new Vector3(0.9f, 1f, roomDimensions.z);
        tableLeft.transform.position = new Vector3(-roomDimensions.x / 2 + 0.9f / 2, 0.5f, 0);
        tableLeft.GetComponent<Renderer>().material = tableMaterial;
        tableLeft.GetComponent<Renderer>().material.color = Color.grey;

       /// Table positioned 0.75 away from the right wall, touching the back wall table
        GameObject tableRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableRight.transform.localScale = new Vector3(1.8f, 1f, 4.6f);
        tableRight.transform.position = new Vector3(roomDimensions.x / 2 - 0.75f - 1.8f / 2, 0.5f, -roomDimensions.z / 2 + 0.75f + 8.3f / 2);
        tableRight.GetComponent<Renderer>().material = tableMaterial;
        tableRight.GetComponent<Renderer>().material.color = Color.grey;

        // The last table 2.4 away from the left wall, touching the back wall table
        GameObject tableLast = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableLast.transform.localScale = new Vector3(1.8f, 1f, 4.6f);
        tableLast.transform.position = new Vector3(-roomDimensions.x / 2 + 2.4f + 1.8f / 2, 0.5f, -roomDimensions.z / 2 + 0.75f + 8.3f / 2);
        tableLast.GetComponent<Renderer>().material = tableMaterial;
        tableLast.GetComponent<Renderer>().material.color = Color.grey;

        // Set parent for all tables (optional, for better hierarchy management)
        GameObject tablesParent = new GameObject("Tables");
        tableFront.transform.parent = tablesParent.transform;
        tableLeft.transform.parent = tablesParent.transform;
        tableRight.transform.parent = tablesParent.transform;
        tableLast.transform.parent = tablesParent.transform;
    }

    // void SpawnRagdoll()
    // {
    //     if (ragdollPrefab != null)
    //     {
    //         ragdollInstance = Instantiate(ragdollPrefab, GetRandomPositionInRoom(), Quaternion.identity);
    //     }
    // }

    // Vector3 GetRandomPositionInRoom()
    // {
    //     float x = Random.Range(-roomDimensions.x / 2, roomDimensions.x / 2);
    //     float z = Random.Range(-roomDimensions.z / 2, roomDimensions.z / 2);
    //     // Assume the ragdoll's "feet" are at y = 0 for simplicity
    //     return new Vector3(x, 0, z);
    // }
    
    // void MoveRagdollToNewPosition()
    // {
    //     // Disable physics while moving to avoid unrealistic behavior
    //     Rigidbody[] rigidbodies = ragdollInstance.GetComponentsInChildren<Rigidbody>();
    //     foreach (var rb in rigidbodies)
    //     {
    //         rb.isKinematic = true;
    //     }

    //     ragdollInstance.transform.position = GetRandomPositionInRoom();

    //     /* // Re-enable physics after moving
    //     foreach (var rb in rigidbodies)
    //     {
    //         rb.isKinematic = false;
    //     } */
    // }

//     void resetMaterial()
//     {
//         // Reset material
//         wallMaterial = defaultMaterial;
//         wallMaterial.color = Color.white;
//         floorMaterial = defaultMaterial;
//         floorMaterial.color = Color.white;
//         tableMaterial = defaultMaterial;
//         tableMaterial.color = Color.white;
//     }
}
