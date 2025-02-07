using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.Image;

public class Polaroid : MonoBehaviour
{
    public Camera polaroidCamera;//拍照所用的相机
    public Transform capturePoint;//拍照的那个点，也是放置照片时的那个点
    public Photo photo;//
    public Transform photoPlacementPoint;//照片在手里的位置

    public float thickness = 0.01f;//创建平面时平面的厚度
    GameObject leftPrimitivePlane, rightPrimitivePlane, topPrimitivePlane, bottomPrimitivePlane, frustumObject;
    MeshFilter leftPrimitivePlaneMF, rightPrimitivePlaneMF, topPrimitivePlaneMF, bottomPrimitivePlaneMF, frustumObjectMF;
    MeshCollider leftPrimitivePlaneMC, rightPrimitivePlaneMC, topPrimitivePlaneMC, bottomPrimitivePlaneMC, frustumObjectMC;

    Vector3 leftUpFrustum, rightUpFrustum, leftDownFrustum, rightDownFrustum, cameraPos;
    Plane leftPlane, rightPlane, topPlane, bottomPlane;
    void Start()
    {
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

        leftPrimitivePlaneMC = leftPrimitivePlane.GetComponent<MeshCollider>();
        leftPrimitivePlaneMC.convex = true;
        leftPrimitivePlaneMC.isTrigger = true;
        leftPrimitivePlaneMC.enabled = false;

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

        leftPrimitivePlaneMF = leftPrimitivePlane.GetComponent<MeshFilter>();
        rightPrimitivePlaneMF = rightPrimitivePlane.GetComponent<MeshFilter>();
        topPrimitivePlaneMF = topPrimitivePlane.GetComponent<MeshFilter>();
        bottomPrimitivePlaneMF = bottomPrimitivePlane.GetComponent<MeshFilter>();
        frustumObjectMF = frustumObject.GetComponent<MeshFilter>();

        leftPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        rightPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        topPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        bottomPrimitivePlane.GetComponent<MeshRenderer>().enabled = false;
        frustumObjectMF.GetComponent<MeshRenderer>().enabled = false;

        var leftChecker = leftPrimitivePlane.AddComponent<CollisionChecker>();
        leftChecker.polaroid = this;
        leftChecker.isPlaneOrFrustum = true;

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

    public List<GameObject> intersectWithPlaneObjects, inFrustumObjects;//视锥内定义为PositivePart
    public List<GameObject> negativePartObjects, positivePartObjects;
    private void SetFrustumAndPlanes()
    {
        float aspectRatio = polaroidCamera.aspect;
        var frustumHeight = 2.0f * polaroidCamera.farClipPlane * Mathf.Tan(polaroidCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var frustumWidth = frustumHeight * aspectRatio;

        leftUpFrustum = new Vector3(-frustumWidth / 2, frustumHeight / 2, polaroidCamera.farClipPlane);
        rightUpFrustum = new Vector3(frustumWidth / 2, frustumHeight / 2, polaroidCamera.farClipPlane);
        leftDownFrustum = new Vector3(-frustumWidth / 2, -frustumHeight / 2, polaroidCamera.farClipPlane);
        rightDownFrustum = new Vector3(frustumWidth / 2, -frustumHeight / 2, polaroidCamera.farClipPlane);

        leftUpFrustum = capturePoint.transform.TransformPoint(leftUpFrustum);
        rightUpFrustum = capturePoint.transform.TransformPoint(rightUpFrustum);
        leftDownFrustum = capturePoint.transform.TransformPoint(leftDownFrustum);
        rightDownFrustum = capturePoint.transform.TransformPoint(rightDownFrustum);

        cameraPos = capturePoint.transform.position;
        Vector3 forwardVector = capturePoint.transform.forward;

        leftPlane = new Plane(cameraPos, leftUpFrustum, leftDownFrustum);
        rightPlane = new Plane(cameraPos, rightDownFrustum, rightUpFrustum);
        topPlane = new Plane(cameraPos, rightUpFrustum, leftUpFrustum);
        bottomPlane = new Plane(cameraPos, leftDownFrustum, rightDownFrustum);

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

        frustumObjectMF.mesh = CreateFrustumObject(cameraPos, rightDownFrustum, rightUpFrustum, leftUpFrustum, leftDownFrustum);
        frustumObjectMC.sharedMesh = frustumObjectMF.mesh;

        //进行碰撞检测
        intersectWithPlaneObjects = new List<GameObject>();
        inFrustumObjects = new List<GameObject>();

        leftPrimitivePlaneMC.enabled = true;
        rightPrimitivePlaneMC.enabled = true;
        topPrimitivePlaneMC.enabled = true;
        bottomPrimitivePlaneMC.enabled = true;
        frustumObjectMC.enabled = true;
        //Debug.Log("collider enabled");

    }
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
    private IEnumerator SetPositivePartAndNegativePartObjects(string positiveOrNegative)
    {
        //给碰撞检测留出时间(时间不够会就可能漏掉需要记录或是删除的物体)
        yield return new WaitForSeconds(0.3f);

        leftPrimitivePlaneMC.enabled = false;
        rightPrimitivePlaneMC.enabled = false;
        topPrimitivePlaneMC.enabled = false;
        bottomPrimitivePlaneMC.enabled = false;
        frustumObjectMC.enabled = false;
        //清空    
        positivePartObjects = new List<GameObject>();
        negativePartObjects = new List<GameObject>();

        intersectWithPlaneObjects = intersectWithPlaneObjects.ToHashSet().ToList();//去重

        for(int i=0;i< inFrustumObjects.Count;i++)
        {
            if (intersectWithPlaneObjects.Contains(inFrustumObjects[i]))
            {
                inFrustumObjects.Remove(inFrustumObjects[i]);//获取全部都在视锥内的物体,
                i--;
            }
        }

        //按照左上右下四个平面，开始不断切分
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

        foreach (Vector3 planeNormal in planeNormals)
        {
            numOfCuttingObjects = positivePartObjects.Count;
            for (int i = numOfCuttingObjects-1; i >= 0; i--)//一定要倒着遍历，否则死循环
            {

                List<GameObject> positivePartAndNegativePart = Slicer.Slice(planeNormal, capturePoint.position, positivePartObjects[i]);

                negativePartObjects.Add(positivePartAndNegativePart[1]);
                positivePartObjects.Add(positivePartAndNegativePart[0]);

                DestroyImmediate(positivePartObjects[i]);
                positivePartObjects.RemoveAt(i);
            }
        }
        //视锥内的也是positivePart
        foreach (GameObject obj in inFrustumObjects)
        {
            GameObject copyObj = Instantiate(obj);
            copyObj.name = obj.name + "In";
            positivePartObjects.Add(copyObj);
        }

        if (positiveOrNegative == "P")
        {
            foreach (GameObject obj in positivePartObjects)
            {
                //绑在photoGameObject上，以后旋转或是放置，操作photoGameObject即可
                obj.transform.SetParent(photo.photoGameObject.transform);
            }
            foreach(GameObject obj in negativePartObjects)
            {
                DestroyImmediate(obj);//此处不需要，直接删去即可
            }
        }
        else
        {
            //foreach (GameObject obj in negativePartObjects)
            //{
            //    obj.transform.SetParent(photo.photoGameObject.transform);
            //}
            foreach (GameObject obj in positivePartObjects)
            {
                DestroyImmediate(obj);//此处不需要，直接删去即可
            }
            //4.Set Photo
            //photo.photoGameObject.transform.SetParent(null);
            for(int i=photo.photoGameObject.transform.childCount-1;i>=0; i--)
            {
                photo.photoGameObject.transform.GetChild(i).transform.SetParent(null);
            }

            //3.Remove Origional obj
            foreach (GameObject obj in intersectWithPlaneObjects) { DestroyImmediate(obj); }
            foreach (GameObject obj in inFrustumObjects) { DestroyImmediate(obj);  }

            //DestroyImmediate(photo.photoGameObject);
        }
        Debug.Log("Finished!!");
    }

    public void World2Photo()
    {
        Debug.Log("Take Photo");
        //1.进行碰撞检测,
        SetFrustumAndPlanes();
        //2.Save PositivePart
        photo = new Photo();
        photo.photoGameObject.SetActive(false);
        photo.photoGameObject.transform.position = capturePoint.position;
        photo.photoGameObject.transform.SetParent(capturePoint);
        StartCoroutine(SetPositivePartAndNegativePartObjects("P"));

    }
    public void Photo2World()
    {
        Debug.Log("Place Photo");
        //1.进行碰撞检测,
        SetFrustumAndPlanes();
        //2.Save NegativePart(还有3，4在SetPositivePartAndNegativePartObjects中)
        StartCoroutine(SetPositivePartAndNegativePartObjects("N"));
    }

    public void RotatePhoto()
    {
        if(photo.photoGameObject != null)
        {

        }
    }
}
public class Photo
{
    //照片的显示

    //视锥内所有物体
    public GameObject photoGameObject = new GameObject("Photo");

}
public class CollisionChecker : MonoBehaviour
{
    public Polaroid polaroid;
    public bool isPlaneOrFrustum;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Cuttable"))
        {
            if (isPlaneOrFrustum) { polaroid.intersectWithPlaneObjects.Add(other.gameObject);}
            else {polaroid.inFrustumObjects.Add(other.gameObject); }
        }
    }
}