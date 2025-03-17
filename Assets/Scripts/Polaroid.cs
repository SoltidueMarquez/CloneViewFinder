using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.Image;

/// <summary>
/// Polaroid类：实现类似拍立得相机的功能，可以拍摄场景中的物体并创建照片
/// </summary>
public class Polaroid : MonoBehaviour
{
    public Camera polaroidCamera;   // 用于拍摄的相机
    public Transform capturePoint;  // 拍摄点，也是放置照片时的位置
    public Photo photo;             // 照片对象
    public Transform photoPlacementPoint;// 照片放置的位置
    public float thickness = 0.01f; // 切割平面时平面的厚度

    // 视锥体四个面的游戏对象引用
    GameObject leftPrimitivePlane, rightPrimitivePlane, topPrimitivePlane, bottomPrimitivePlane, frustumObject;
    // 视锥体各个面的网格过滤器
    MeshFilter leftPrimitivePlaneMF, rightPrimitivePlaneMF, topPrimitivePlaneMF, bottomPrimitivePlaneMF, frustumObjectMF;
    // 视锥体各个面的碰撞器
    MeshCollider leftPrimitivePlaneMC, rightPrimitivePlaneMC, topPrimitivePlaneMC, bottomPrimitivePlaneMC, frustumObjectMC;
    
    // 视锥体四个角点的世界坐标
    Vector3 leftUpFrustum, rightUpFrustum, leftDownFrustum, rightDownFrustum, cameraPos;
    // 视锥体的四个平面
    Plane leftPlane, rightPlane, topPlane, bottomPlane;

    /// <summary>
    /// 初始化视锥体的各个面和碰撞器
    /// </summary>
    void Start()
    {
        // 创建四个视锥体平面和视锥体对象
        leftPrimitivePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        leftPrimitivePlane.name = "LeftCameraPlane";
        rightPrimitivePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        rightPrimitivePlane.name = "RightCameraPlane";
        topPrimitivePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        topPrimitivePlane.name = "TopCameraPlane";
        bottomPrimitivePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        bottomPrimitivePlane.name = "BottomCameraPlane";
        frustumObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        frustumObject.name = "FrustumObject";

        // 初始化各个面的碰撞器
        leftPrimitivePlaneMC = leftPrimitivePlane.GetComponent<MeshCollider>();
        leftPrimitivePlaneMC.convex = true;     // 设置为凸多边形以支持切割
        leftPrimitivePlaneMC.isTrigger = true;  // 设置为触发器，用于检测碰撞
        leftPrimitivePlaneMC.enabled = false;   // 初始禁用，需要时启用

        rightPrimitivePlaneMC = rightPrimitivePlane.GetComponent<MeshCollider>();
        rightPrimitivePlaneMC.convex = true;
        rightPrimitivePlaneMC.isTrigger = true;
        rightPrimitivePlaneMC.enabled = false;

        topPrimitivePlaneMC = topPrimitivePlane.GetComponent<MeshCollider>();
        topPrimitivePlaneMC.convex = true;
        topPrimitivePlaneMC.isTrigger = true;
        topPrimitivePlaneMC.enabled = false;

        bottomPrimitivePlaneMC = bottomPrimitivePlane.GetComponent<MeshCollider>();
        bottomPrimitivePlaneMC.convex = true;
        bottomPrimitivePlaneMC.isTrigger = true;
        bottomPrimitivePlaneMC.enabled = false;

        frustumObjectMC = frustumObject.GetComponent<MeshCollider>();
        frustumObjectMC.convex = true;
        frustumObjectMC.isTrigger = true;
        frustumObjectMC.enabled = false;

        // 获取各个面的网格过滤器
        leftPrimitivePlaneMF = leftPrimitivePlane.GetComponent<MeshFilter>();
        rightPrimitivePlaneMF = rightPrimitivePlane.GetComponent<MeshFilter>();
        topPrimitivePlaneMF = topPrimitivePlane.GetComponent<MeshFilter>();
        bottomPrimitivePlaneMF = bottomPrimitivePlane.GetComponent<MeshFilter>();
        frustumObjectMF = frustumObject.GetComponent<MeshFilter>();

        // 禁用所有面的渲染器
        leftPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        rightPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        topPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        bottomPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        frustumObjectMF.GetComponent<MeshRenderer>().enabled = false;

        // 添加碰撞检测器到各个面，用于记录相交对象
        var leftChecker = leftPrimitivePlane.AddComponent<CollisionChecker>();
        leftChecker.polaroid = this;
        leftChecker.isPlaneOrFrustum = true;    // 标记为视锥体平面

        var rightChecker = rightPrimitivePlane.AddComponent<CollisionChecker>();
        rightChecker.polaroid = this;
        rightChecker.isPlaneOrFrustum = true;

        var topChecker = topPrimitivePlane.AddComponent<CollisionChecker>();
        topChecker.polaroid = this;
        topChecker.isPlaneOrFrustum = true;

        var bottomChecker = bottomPrimitivePlane.AddComponent<CollisionChecker>();
        bottomChecker.polaroid = this;
        bottomChecker.isPlaneOrFrustum = true;

        var frustumChecker = frustumObject.AddComponent<CollisionChecker>();
        frustumChecker.polaroid = this;
        frustumChecker.isPlaneOrFrustum = false;
    }

    // 存储与平面相交的对象和视锥体内的对象
    public List<GameObject> intersectWithPlaneObjects, inFrustumObjects;
    // 存储完全在视锥体外的对象和视锥体内的对象
    public List<GameObject> negativePartObjects, positivePartObjects;

    /// <summary>
    /// 设置视锥体和切割平面
    /// </summary>
    private void SetFrustumAndPlanes()
    {
        // 计算视锥体的尺寸
        float aspectRatio = polaroidCamera.aspect;
        var frustumHeight = 2.0f * polaroidCamera.farClipPlane * Mathf.Tan(polaroidCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var frustumWidth = frustumHeight * aspectRatio;

        // 计算视锥体的四个角点
        leftUpFrustum = new Vector3(-frustumWidth / 2, frustumHeight / 2, polaroidCamera.farClipPlane);
        rightUpFrustum = new Vector3(frustumWidth / 2, frustumHeight / 2, polaroidCamera.farClipPlane);
        leftDownFrustum = new Vector3(-frustumWidth / 2, -frustumHeight / 2, polaroidCamera.farClipPlane);
        rightDownFrustum = new Vector3(frustumWidth / 2, -frustumHeight / 2, polaroidCamera.farClipPlane);

        // 将视锥体角点转换到世界坐标系
        leftUpFrustum = capturePoint.transform.TransformPoint(leftUpFrustum);
        rightUpFrustum = capturePoint.transform.TransformPoint(rightUpFrustum);
        leftDownFrustum = capturePoint.transform.TransformPoint(leftDownFrustum);
        rightDownFrustum = capturePoint.transform.TransformPoint(rightDownFrustum);

        cameraPos = capturePoint.transform.position;
        Vector3 forwardVector = capturePoint.transform.forward;

        // 创建四个切割平面
        leftPlane = new Plane(cameraPos, leftUpFrustum, leftDownFrustum);
        rightPlane = new Plane(cameraPos, rightDownFrustum, rightUpFrustum);
        topPlane = new Plane(cameraPos, rightUpFrustum, leftUpFrustum);
        bottomPlane = new Plane(cameraPos, leftDownFrustum, rightDownFrustum);

        // 创建各个面的网格
        var leftOffset = leftPlane.normal * thickness;
        leftPrimitivePlaneMF.mesh = CreateBoxMesh(cameraPos, leftUpFrustum, (leftUpFrustum + leftDownFrustum) / 2, leftDownFrustum,
        leftDownFrustum + leftOffset, ((leftUpFrustum + leftDownFrustum) / 2) + leftOffset, leftUpFrustum + leftOffset, cameraPos + leftOffset);
        leftPrimitivePlaneMC.sharedMesh = leftPrimitivePlaneMF.mesh;

        var rightOffset = rightPlane.normal * thickness;
        rightPrimitivePlaneMF.mesh = CreateBoxMesh(cameraPos, rightDownFrustum, (rightUpFrustum + rightDownFrustum) / 2, rightUpFrustum,
        rightUpFrustum + rightOffset, ((rightUpFrustum + rightDownFrustum) / 2) + rightOffset, rightDownFrustum + rightOffset, cameraPos + rightOffset);
        rightPrimitivePlaneMC.sharedMesh = rightPrimitivePlaneMF.mesh;

        var topOffset = topPlane.normal * thickness;
        topPrimitivePlaneMF.mesh = CreateBoxMesh(cameraPos, rightUpFrustum, (leftUpFrustum + rightUpFrustum) / 2, leftUpFrustum,
        leftUpFrustum + topOffset, ((leftUpFrustum + rightUpFrustum) / 2) + topOffset, rightUpFrustum + topOffset, cameraPos + topOffset);
        topPrimitivePlaneMC.sharedMesh = topPrimitivePlaneMF.mesh;

        var bottomOffset = bottomPlane.normal * thickness;
        bottomPrimitivePlaneMF.mesh = CreateBoxMesh(cameraPos, leftDownFrustum, (leftDownFrustum + rightDownFrustum) / 2, rightDownFrustum,
        rightDownFrustum + bottomOffset, ((leftDownFrustum + rightDownFrustum) / 2) + bottomOffset, leftDownFrustum + bottomOffset, cameraPos + bottomOffset);
        bottomPrimitivePlaneMC.sharedMesh = bottomPrimitivePlaneMF.mesh;

        // 创建视锥体对象的网格
        frustumObjectMF.mesh = CreateFrustumObject(cameraPos, rightDownFrustum, rightUpFrustum, leftUpFrustum, leftDownFrustum);
        frustumObjectMC.sharedMesh = frustumObjectMF.mesh;

        // 初始化对象列表
        intersectWithPlaneObjects = new List<GameObject>();
        inFrustumObjects = new List<GameObject>();

        // 启用所有碰撞器
        leftPrimitivePlaneMC.enabled = true;
        rightPrimitivePlaneMC.enabled = true;
        topPrimitivePlaneMC.enabled = true;
        bottomPrimitivePlaneMC.enabled = true;
        frustumObjectMC.enabled = true;
    }

    /// <summary>
    /// 创建一个盒体网格
    /// </summary>
    private Mesh CreateBoxMesh(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6, Vector3 v7, Vector3 v8)
    {
        Vector3[] vertices = new Vector3[] {
            v1,
            v2,
            v3,
            v4,
            v5,
            v6,
            v7,
            v8};

        int[] triangles = new int[] {
            0, 1, 2,
            0, 2, 3,

            3, 2, 5,
            3, 5, 3,

            2, 1, 6,
            2, 6, 5,

            7, 4, 5,
            7, 5, 6,

            0, 1, 6,
            0, 6, 7,

            0, 7, 4,
            0, 4, 3};

        var mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
        };

        return mesh;
    }

    /// <summary>
    /// 创建视锥体对象的网格
    /// </summary>
    private Mesh CreateFrustumObject(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5)
    {
        Vector3[] vertices = new Vector3[] {
            v1,
            v2,
            v3,
            v4,
            v5
        };

        int[] triangles = new int[] {
            0, 2, 1,

            4, 1, 2,
            4, 2, 3,

            0, 4, 3,

            0, 1, 4,

            0, 3, 2,
        };

        var mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
        };

        return mesh;
    }

    /// <summary>
    /// 设置正负部分对象
    /// </summary>
    private IEnumerator SetPositivePartAndNegativePartObjects(string positiveOrNegative)
    {
        // 等待碰撞检测完成
        yield return new WaitForSeconds(0.3f);

        // 禁用所有碰撞器
        leftPrimitivePlaneMC.enabled = false;
        rightPrimitivePlaneMC.enabled = false;
        topPrimitivePlaneMC.enabled = false;
        bottomPrimitivePlaneMC.enabled = false;
        frustumObjectMC.enabled = false;

        // 初始化正负部分对象列表
        positivePartObjects = new List<GameObject>();
        negativePartObjects = new List<GameObject>();

        // 去重相交对象
        intersectWithPlaneObjects = intersectWithPlaneObjects.ToHashSet().ToList();

        // 从视锥体内对象中移除与平面相交的对象
        for(int i=0;i< inFrustumObjects.Count;i++)
        {
            if (intersectWithPlaneObjects.Contains(inFrustumObjects[i]))
            {
                inFrustumObjects.Remove(inFrustumObjects[i]);
                i--;
            }
        }

        // 复制与平面相交的对象
        List<Vector3> planeNormals = new List<Vector3> { leftPlane.normal, topPlane.normal, rightPlane.normal, bottomPlane.normal };
        int numOfCuttingObjects;
        foreach(GameObject obj in intersectWithPlaneObjects)
        {
            GameObject newObj = Instantiate(obj,obj.transform.parent);
            newObj.transform.position = obj.transform.position;
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;

            positivePartObjects.Add(newObj);
        }

        // 对每个平面进行切割
        foreach (Vector3 planeNormal in planeNormals)
        {
            numOfCuttingObjects = positivePartObjects.Count;
            for (int i = numOfCuttingObjects-1; i >= 0; i--)
            {
                List<GameObject> positivePartAndNegativePart = Slicer.Slice(planeNormal, capturePoint.position, positivePartObjects[i]);

                negativePartObjects.Add(positivePartAndNegativePart[1]);
                positivePartObjects.Add(positivePartAndNegativePart[0]);

                DestroyImmediate(positivePartObjects[i]);
                positivePartObjects.RemoveAt(i);
            }
        }

        // 复制视锥体内的对象
        foreach (GameObject obj in inFrustumObjects)
        {
            GameObject copyObj = Instantiate(obj);
            copyObj.name = obj.name + "In";
            positivePartObjects.Add(copyObj);
        }

        // 根据参数处理正负部分对象
        if (positiveOrNegative == "P")
        {
            // 将正部分对象设置为照片的子对象
            foreach (GameObject obj in positivePartObjects)
            {
                obj.transform.SetParent(photo.photoGameObject.transform);
            }
            // 销毁负部分对象
            foreach(GameObject obj in negativePartObjects)
            {
                DestroyImmediate(obj);
            }
        }
        else
        {
            // 销毁正部分对象
            foreach (GameObject obj in positivePartObjects)
            {
                DestroyImmediate(obj);
            }

            // 将照片的子对象设置为场景根对象
            for(int i=photo.photoGameObject.transform.childCount-1;i>=0; i--)
            {
                photo.photoGameObject.transform.GetChild(i).transform.SetParent(null);
            }

            // 销毁原始对象
            foreach (GameObject obj in intersectWithPlaneObjects) { DestroyImmediate(obj); }
            foreach (GameObject obj in inFrustumObjects) { DestroyImmediate(obj);  }
        }
        Debug.Log("Finished!!");
    }

    /// <summary>
    /// 拍摄照片：将场景中的物体转换为照片
    /// </summary>
    public void World2Photo()
    {
        Debug.Log("Take Photo");
        // 设置视锥体和切割平面
        SetFrustumAndPlanes();
        // 保存正部分对象
        photo = new Photo();
        photo.photoGameObject.SetActive(false);
        photo.photoGameObject.transform.position = capturePoint.position;
        photo.photoGameObject.transform.SetParent(capturePoint);
        StartCoroutine(SetPositivePartAndNegativePartObjects("P"));
    }

    /// <summary>
    /// 放置照片：将照片中的物体放回场景
    /// </summary>
    public void Photo2World()
    {
        Debug.Log("Place Photo");
        // 设置视锥体和切割平面
        SetFrustumAndPlanes();
        // 保存负部分对象
        StartCoroutine(SetPositivePartAndNegativePartObjects("N"));
    }

    /// <summary>
    /// 旋转照片
    /// </summary>
    public void RotatePhoto()
    {
        if(photo.photoGameObject != null)
        {
            // TODO: 实现照片旋转功能
        }
    }
}

/// <summary>
/// Photo类：表示照片对象
/// </summary>
public class Photo
{
    // 照片的游戏对象
    public GameObject photoGameObject = new GameObject("Photo");
}

/// <summary>
/// CollisionChecker类：用于检测碰撞
/// </summary>
public class CollisionChecker : MonoBehaviour
{
    public Polaroid polaroid;
    public bool isPlaneOrFrustum;

    /// <summary>
    /// 当触发器与其他物体相交时调用
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Cuttable"))
        {
            if (isPlaneOrFrustum) { polaroid.intersectWithPlaneObjects.Add(other.gameObject);}
            else {polaroid.inFrustumObjects.Add(other.gameObject); }
        }
    }
}