using UnityEngine;

public class ArrowDrawer : MonoBehaviour
{
    // private Vector3 startPos = Vector3.zero; // Arrow start (tail)
    // private Vector3 endPos = new Vector3(0, 0, 5); // Arrow end (tip)

    private GameObject arrowBody;  // Arrow shaft (cylinder)
    private GameObject arrowHead;  // Arrowhead (cone)

    public float arrowHeadSize = 0.3f;  // Size of the arrowhead
    public float arrowBodyWidth = 0.1f; // Width of the arrow shaft

    public void CreateArrow(int droneId, Vector3 startPos, Vector3 endPos, string color)
    {   
        
        // Create Arrow Body (Cylinder)
        arrowBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        UpdateArrowBody(startPos, endPos, color);
        arrowBody.name = "ArrowBody_" + droneId;
        // arrowBody.transform.parent = transform; // Attach to this GameObject

        // 2️⃣ Create Arrowhead (Cone)
        arrowHead = CreateCone(arrowHeadSize, arrowHeadSize * 4f);
        UpdateArrowHead(startPos, endPos, color);
        arrowHead.name = "ArrowHead_" + droneId;

        // Initial position update
        // UpdateArrow();
    }

    void Update()
    {
        // UpdateArrow(); // Update dynamically
    }
    
    
    public void UpdateArrow(Vector3 startPos, Vector3 endPos, string color)
    {
        UpdateArrowBody(startPos, endPos, color);
        UpdateArrowHead(startPos, endPos, color);
    }

    public void DestroyArrow()
    {   
        // destroy the inner two game objects
        Destroy(arrowBody);
        Destroy(arrowHead);
    }

    GameObject CreateCone(float radius, float height)
    {
        Mesh coneMesh = new Mesh();
        int segments = 20;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3 * 2];

        vertices[0] = Vector3.up * height; // Tip of the cone
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        }
        vertices[segments + 1] = Vector3.zero; // Center of the base

        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = (i + 1) % segments + 1;
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[triIndex++] = segments + 1;
            triangles[triIndex++] = (i + 1) % segments + 1;
            triangles[triIndex++] = i + 1;
        }

        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();

        GameObject cone = new GameObject("Arrowhead");
        MeshFilter meshFilter = cone.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = cone.AddComponent<MeshRenderer>();
        meshFilter.mesh = coneMesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));

        return cone;
    }

    private void UpdateArrowHead(Vector3 startPos, Vector3 endPos, string color)
    {   
        // dont' show on screen (no meaning when drone doesn't move)
        if (startPos == endPos)
        {
            arrowHead.GetComponent<MeshRenderer>().enabled = false;
        }
        else
        {
            arrowHead.GetComponent<MeshRenderer>().enabled = true;
        }
        Vector3 direction = endPos - startPos;
        // float length = direction.magnitude;
        direction.Normalize(); // Ensure it's a unit vector
        Color myUnityColor = Utils.CreateColorFromCsvName(color);
        // update arrowHead
        arrowHead.transform.position = startPos + (endPos - startPos) / 2f; // Attach to this GameObject
        arrowHead.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        arrowHead.GetComponent<MeshRenderer>().material.color = myUnityColor;
    }

    private void UpdateArrowBody(Vector3 startPos, Vector3 endPos, string color)
    {
        arrowBody.transform.position = startPos + (endPos - startPos) / 2f;
        // Debug.Log(arrowBody.transform.position);
        Vector3 direction = endPos - startPos;
        float length = direction.magnitude;
        direction.Normalize(); // Ensure it's a unit vector
        // Debug.Log(direction.ToString());
        arrowBody.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        arrowBody.transform.localScale = new Vector3(arrowBodyWidth, length/2f, arrowBodyWidth); // Scale to match length
        Color myUnityColor = Utils.CreateColorFromCsvName(color); 
        arrowBody.GetComponent<MeshRenderer>().material.color = myUnityColor;
    }
}
