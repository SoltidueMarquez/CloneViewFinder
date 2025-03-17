using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;


public class Slicer : MonoBehaviour
{
    static Mesh positivePartMesh = new Mesh();
    static Mesh negativePartMesh = new Mesh();
    static List<Vector3> positivePartVertices;
    static Dictionary<string, int> positivePartVerticesID2Inndexs;
    static List<Vector3> positivePartNormals;
    static List<Vector2> positivePartUvs;
    static Dictionary<int, List<int>> positivePartSubMeshTriangles;

    static List<Vector3> negativePartVertices;
    static Dictionary<string, int> negativePartVerticesID2Inndexs;
    static List<Vector3> negativePartNormals;
    static List<Vector2> negativePartUvs;
    static Dictionary<int, List<int>> negativePartSubMeshTriangles;

    static List<string> alongPlaneVerticesIDs;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="gameObject"></param>
    /// <returns>�����г���������壬Positive��Negtivate two parts,positive part is the part same with plane normal</returns>
    public static List<GameObject> Slice(Vector3 planeNormal, Vector3 planePoint, GameObject gameObject)
    {
        positivePartVertices = new List<Vector3>();
        positivePartVerticesID2Inndexs = new Dictionary<string, int>();//sharedVertex,Ҫȥ��
        positivePartNormals = new List<Vector3>();
        positivePartUvs = new List<Vector2>();
        positivePartSubMeshTriangles = new Dictionary<int, List<int>>();

        negativePartVertices = new List<Vector3>();
        negativePartVerticesID2Inndexs = new Dictionary<string, int>();
        negativePartNormals = new List<Vector3>();
        negativePartUvs = new List<Vector2>();
        negativePartSubMeshTriangles = new Dictionary<int, List<int>>();

        alongPlaneVerticesIDs = new List<string>();

        //--->����һ���µ�Mesh
        Mesh originalMesh = gameObject.GetComponent<MeshFilter>().mesh;
        if (originalMesh == null) Debug.LogError("there is no mesh");
        Mesh readyToCutGameObjectMesh = new Mesh();
        readyToCutGameObjectMesh.vertices = originalMesh.vertices;
        readyToCutGameObjectMesh.triangles = originalMesh.triangles;
        readyToCutGameObjectMesh.normals = originalMesh.normals;
        readyToCutGameObjectMesh.tangents = originalMesh.tangents;
        readyToCutGameObjectMesh.uv = originalMesh.uv;
        readyToCutGameObjectMesh.uv2 = originalMesh.uv2;
        readyToCutGameObjectMesh.uv3 = originalMesh.uv3;
        readyToCutGameObjectMesh.uv4 = originalMesh.uv4;
        readyToCutGameObjectMesh.colors = originalMesh.colors;
        readyToCutGameObjectMesh.bounds = originalMesh.bounds;

        readyToCutGameObjectMesh.subMeshCount = originalMesh.subMeshCount;
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            readyToCutGameObjectMesh.SetTriangles(originalMesh.GetTriangles(i), i);
        }

        //-->�任��������ϵ�¶����ƽ��Ϊ���и�����ľֲ�����ϵ
        //Transform the normal so that it is aligned with the object we are slicing's transform.
        Vector3 transformedNormal = ((Vector3)(gameObject.transform.localToWorldMatrix.transpose * planeNormal)).normalized;//??��ʽ??
        //Get the enter position relative to the object we're cutting's local transform
        Vector3 transformedStartingPoint = gameObject.transform.InverseTransformPoint(planePoint);//??��ʽ??
        Plane plane = new Plane();
        plane.SetNormalAndPosition(
                transformedNormal,
                transformedStartingPoint);


        //--->����submesh
        for (int subMeshIndex = 0; subMeshIndex < readyToCutGameObjectMesh.subMeshCount; subMeshIndex++)
        {
            int[] subMeshTriangleIndices = readyToCutGameObjectMesh.GetTriangles(subMeshIndex);

            for (int j = 0; j < subMeshTriangleIndices.Length; j += 3)
            {
                int[] vertexIndexs = new int[3] { subMeshTriangleIndices[j], subMeshTriangleIndices[j + 1], subMeshTriangleIndices[j + 2] };
                List<string> vertexIDs = new List<string> { vertexIndexs[0].ToString(), vertexIndexs[1].ToString(), vertexIndexs[2].ToString() };
                List<Vector3> vertices = new List<Vector3> {
                    readyToCutGameObjectMesh.vertices[vertexIndexs[0]],
                    readyToCutGameObjectMesh.vertices[vertexIndexs[1]],
                    readyToCutGameObjectMesh.vertices[vertexIndexs[2]] };
                List<Vector3> normals = new List<Vector3> {
                    readyToCutGameObjectMesh.normals[vertexIndexs[0]],
                    readyToCutGameObjectMesh.normals[vertexIndexs[1]],
                    readyToCutGameObjectMesh.normals[vertexIndexs[2]] };
                List<Vector2> uv = new List<Vector2> {
                    readyToCutGameObjectMesh.uv[vertexIndexs[0]],
                    readyToCutGameObjectMesh.uv[vertexIndexs[1]],
                    readyToCutGameObjectMesh.uv[vertexIndexs[2]] };

                bool[] vertexSides = new bool[3] {
                    plane.GetSide(vertices[0]),
                    plane.GetSide(vertices[1]),
                    plane.GetSide(vertices[2])
                };
                //All three vertices are on the same side,������ȡһ����֪��λ����һ��
                if (vertexSides[0] == vertexSides[1] && vertexSides[0] == vertexSides[2])
                {
                    //List<int> onPlaneVertexIndexs = new List<int>();
                    //for(int i = 0; i < 3; i++)
                    //{
                    //    if (plane.GetDistanceToPoint(vertices[i]) < 0.00002f)
                    //    {
                    //        onPlaneVertexIndexs.Add(i);
                    //    }
                    //}
                    //if(onPlaneVertexIndexs.Count>1) alongPlaneVerticesIDs.AddRange(new List<string> { vertexIDs[onPlaneVertexIndexs[0]], vertexIDs[onPlaneVertexIndexs[1]] });

                    AddSubMeshTriangleInfo(vertexSides[0], subMeshIndex, vertexIDs, vertices, normals, uv);//������ֿ��ܻ�ɾ��vertices,�������жϾ���
                }
                else
                {
                    //--->��ȡ�ص㣨��ƽ�����еĵ㣩��������Ҫ���¼���UV��Normal��
                    Vector3 intersectionVertex1, intersectionNormal1;
                    string intersectionVertex1ID;
                    Vector2 intersectionUV1;

                    Vector3 intersectionVertex2, intersectionNormal2;
                    string intersectionVertex2ID;
                    Vector2 intersectionUV2;

                    int singleSideVertexIndex = 0;
                    int doubleSideVertexIndex = 0;
                    //�������Լ�����һ����Ǹ���������������������ȡ����
                    for (int i = 0; i < 3; i++)
                    {
                        if (vertexSides[i] != vertexSides[(i + 3 - 1) % 3] && vertexSides[i] != vertexSides[(i + 1) % 3])
                        {
                            singleSideVertexIndex = i;
                        }
                    }

                    float distance, normalizedDistance;

                    doubleSideVertexIndex = (singleSideVertexIndex + 1) % 3;
                    plane.Raycast(new Ray(vertices[singleSideVertexIndex], (vertices[doubleSideVertexIndex] - vertices[singleSideVertexIndex]).normalized), out distance);
                    normalizedDistance = distance / (vertices[doubleSideVertexIndex] - vertices[singleSideVertexIndex]).magnitude;//��һ��������ֵ��ϵ��
                    intersectionVertex1 = Vector3.Lerp(vertices[singleSideVertexIndex], vertices[doubleSideVertexIndex], normalizedDistance);
                    intersectionNormal1 = Vector3.Lerp(normals[singleSideVertexIndex], normals[doubleSideVertexIndex], normalizedDistance);
                    intersectionUV1 = Vector2.Lerp(uv[singleSideVertexIndex], uv[doubleSideVertexIndex], normalizedDistance);
                    intersectionVertex1ID = vertexIndexs[singleSideVertexIndex] > vertexIndexs[doubleSideVertexIndex] ?
                        vertexIndexs[doubleSideVertexIndex].ToString() + "-" + vertexIndexs[singleSideVertexIndex] :
                        vertexIndexs[singleSideVertexIndex].ToString() + "-" + vertexIndexs[doubleSideVertexIndex];

                    doubleSideVertexIndex = (singleSideVertexIndex + 2) % 3;
                    plane.Raycast(new Ray(vertices[singleSideVertexIndex], (vertices[doubleSideVertexIndex] - vertices[singleSideVertexIndex]).normalized), out distance);
                    normalizedDistance = distance / (vertices[doubleSideVertexIndex] - vertices[singleSideVertexIndex]).magnitude;//��һ��������ֵ��ϵ��
                    intersectionVertex2 = Vector3.Lerp(vertices[singleSideVertexIndex], vertices[doubleSideVertexIndex], normalizedDistance);
                    intersectionNormal2 = Vector3.Lerp(normals[singleSideVertexIndex], normals[doubleSideVertexIndex], normalizedDistance);
                    intersectionUV2 = Vector2.Lerp(uv[singleSideVertexIndex], uv[doubleSideVertexIndex], normalizedDistance);
                    intersectionVertex2ID = vertexIndexs[singleSideVertexIndex] > vertexIndexs[doubleSideVertexIndex] ?
                        vertexIndexs[doubleSideVertexIndex].ToString() + "-" + vertexIndexs[singleSideVertexIndex] :
                        vertexIndexs[singleSideVertexIndex].ToString() + "-" + vertexIndexs[doubleSideVertexIndex];

                    //--->���������Ľص���ԭʼ�����ι�������������
                    //����˳ʱ��˳��
                    AddSubMeshTriangleInfo(vertexSides[singleSideVertexIndex], subMeshIndex,
                        new List<string> { vertexIDs[singleSideVertexIndex], intersectionVertex1ID, intersectionVertex2ID },
                        new List<Vector3> { vertices[singleSideVertexIndex], intersectionVertex1, intersectionVertex2 },
                        new List<Vector3> { normals[singleSideVertexIndex], intersectionNormal1, intersectionNormal2 },
                        new List<Vector2> { uv[singleSideVertexIndex], intersectionUV1, intersectionUV2 });

                    AddSubMeshTriangleInfo(vertexSides[(singleSideVertexIndex + 1) % 3], subMeshIndex,
                        new List<string> { vertexIDs[(singleSideVertexIndex + 1) % 3], intersectionVertex2ID, intersectionVertex1ID },
                        new List<Vector3> { vertices[(singleSideVertexIndex + 1) % 3], intersectionVertex2, intersectionVertex1 },
                        new List<Vector3> { normals[(singleSideVertexIndex + 1) % 3], intersectionNormal2, intersectionNormal1 },
                        new List<Vector2> { uv[(singleSideVertexIndex + 1) % 3], intersectionUV2, intersectionUV1 });

                    AddSubMeshTriangleInfo(vertexSides[(singleSideVertexIndex + 2) % 3], subMeshIndex,
                        new List<string> { vertexIDs[(singleSideVertexIndex + 1) % 3], vertexIDs[(singleSideVertexIndex + 2) % 3], intersectionVertex2ID },
                        new List<Vector3> { vertices[(singleSideVertexIndex + 1) % 3], vertices[(singleSideVertexIndex + 2) % 3], intersectionVertex2 },
                        new List<Vector3> { normals[(singleSideVertexIndex + 1) % 3], normals[(singleSideVertexIndex + 2) % 3], intersectionNormal2 },
                        new List<Vector2> { uv[(singleSideVertexIndex + 1) % 3], uv[(singleSideVertexIndex + 2) % 3], intersectionUV2 });

                    //���ں�������и���Ѱ������
                    alongPlaneVerticesIDs.AddRange(new List<string> { intersectionVertex1ID, intersectionVertex2ID });
                }
            }
        }
        //--->�������нص�Թ����µ�������������
        Vector3 centerVertex = Vector3.zero;
        foreach (string id in alongPlaneVerticesIDs)
        {
            centerVertex += positivePartVertices[positivePartVerticesID2Inndexs[id]];
        }
        centerVertex /= alongPlaneVerticesIDs.Count;
        Vector3 centerVertexNormal;
        Vector2 centerVertexUV = new(0f, 0f);

        int positivePartSubMeshIndex = positivePartSubMeshTriangles.Count;
        int negativePartSubMeshIndex = negativePartSubMeshTriangles.Count;

        string centerVertexPositivePartID = "-2";//positivePartVertices.Count.ToString();��Щ���ܻ�����е��ظ�������
        string centerVertexNegativePartID = "-1";//negativePartVertices.Count.ToString();
        for (int i = 0; i < alongPlaneVerticesIDs.Count; i = i + 2)
        {
            string alongPlaneVertexID1 = alongPlaneVerticesIDs[i];
            string alongPlaneVertexID2 = alongPlaneVerticesIDs[i + 1];
            //��postive���ֿ��Ƿ���Ҫ��ת
            if (Vector3.Dot(Vector3.Cross(positivePartVertices[positivePartVerticesID2Inndexs[alongPlaneVertexID1]] - centerVertex, positivePartVertices[positivePartVerticesID2Inndexs[alongPlaneVertexID2]] - centerVertex), -plane.normal) < 0)
            {
                //Debug.Log("reverse");
                (alongPlaneVertexID2, alongPlaneVertexID1) = (alongPlaneVertexID1, alongPlaneVertexID2);
            }

            int alongPlaneVertexInPositivePartIndex1 = positivePartVerticesID2Inndexs[alongPlaneVertexID1];
            int alongPlaneVertexInPositivePartIndex2 = positivePartVerticesID2Inndexs[alongPlaneVertexID2];
            centerVertexNormal = -plane.normal;
            AddSubMeshTriangleInfo(true, positivePartSubMeshIndex,
                new List<string> { alongPlaneVertexID1, alongPlaneVertexID2, centerVertexPositivePartID },
                new List<Vector3> { positivePartVertices[alongPlaneVertexInPositivePartIndex1], positivePartVertices[alongPlaneVertexInPositivePartIndex2], centerVertex },
                new List<Vector3> { positivePartNormals[alongPlaneVertexInPositivePartIndex1], positivePartNormals[alongPlaneVertexInPositivePartIndex2], centerVertexNormal },
                new List<Vector2> { positivePartUvs[alongPlaneVertexInPositivePartIndex1], positivePartUvs[alongPlaneVertexInPositivePartIndex2], centerVertexUV });

            int alongPlaneVertexInNegativePartIndex1 = negativePartVerticesID2Inndexs[alongPlaneVertexID1];
            int alongPlaneVertexInNegativePartIndex2 = negativePartVerticesID2Inndexs[alongPlaneVertexID2];
            centerVertexNormal = plane.normal;
            AddSubMeshTriangleInfo(false, negativePartSubMeshIndex,
                new List<string> { alongPlaneVertexID2, alongPlaneVertexID1, centerVertexNegativePartID },
                new List<Vector3> { negativePartVertices[alongPlaneVertexInNegativePartIndex2], negativePartVertices[alongPlaneVertexInNegativePartIndex1], centerVertex },
                new List<Vector3> { negativePartNormals[alongPlaneVertexInNegativePartIndex2], negativePartNormals[alongPlaneVertexInNegativePartIndex1], centerVertexNormal },
                new List<Vector2> { negativePartUvs[alongPlaneVertexInNegativePartIndex2], negativePartUvs[alongPlaneVertexInNegativePartIndex1], centerVertexUV });
        }
        //-->
        SetMesh("PositivePart");
        SetMesh("NegativePart");

        Material mat = gameObject.GetComponent<MeshRenderer>().material;//����ֻȡ�˵�һ��
        var originalCols = gameObject.GetComponents<Collider>();//ɾ��֮ǰ��colliders
        foreach (var col in originalCols)
        {
            DestroyImmediate(col);//������destory������������һ֡�Ż�ִ��
        }
        //����mesh,material,collider
        GameObject positivePartGameObject = Instantiate(gameObject);
        positivePartGameObject.name = gameObject.name + "P";
        //positivePartGameObject.SetActive(false);
        positivePartGameObject.GetComponent<MeshFilter>().mesh = positivePartMesh;

        Material[] mats = new Material[positivePartMesh.subMeshCount];
        for (int i = 0; i < positivePartMesh.subMeshCount; i++)
        {
            mats[i] = mat;
        }
        positivePartGameObject.GetOrAddComponent<MeshRenderer>().materials = mats;//һ��Ҫ����Material

        var collider = positivePartGameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = positivePartMesh;
        collider.convex = true;

        GameObject negativePartGameObject = Instantiate(gameObject);
        negativePartGameObject.name = gameObject.name + "N";
        //negativePartGameObject.SetActive(false);
        negativePartGameObject.GetComponent<MeshFilter>().mesh = negativePartMesh;
        mats = new Material[negativePartMesh.subMeshCount];
        for (int i = 0; i < negativePartMesh.subMeshCount; i++)
        {
            mats[i] = mat;
        }
        negativePartGameObject.GetOrAddComponent<MeshRenderer>().materials = mats;

        collider = negativePartGameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = negativePartMesh;
        collider.convex = true;


        return new List<GameObject> { positivePartGameObject, negativePartGameObject };
    }
    private static void SetMesh(string whichPart)
    {

        if (whichPart == "PositivePart")
        {
            positivePartMesh = new Mesh();//����,����֮ǰ�����ݻ�Ӱ�������õĶ������ݵȵ�
            //if (positivePartVertices.Count == 0) return;
            positivePartMesh.SetVertices(positivePartVertices);
            positivePartMesh.SetNormals(positivePartNormals);
            positivePartMesh.SetUVs(0, positivePartUvs); //UV0(ͨ��0): ��Ҫ���ڻ�������ӳ�䣨diffuse texture, albedo�ȣ�,UV1(ͨ��1): ͨ�����ڹ�����ͼ��lightmap��,UV2(ͨ��2): �����ڶ����Ч����ϸ����ͼ
            positivePartMesh.SetUVs(1, positivePartUvs);

            positivePartMesh.subMeshCount = positivePartSubMeshTriangles.Count;
            int i = 0;
            foreach (KeyValuePair<int, List<int>> subMeshTriangle in positivePartSubMeshTriangles)
            {
                positivePartMesh.SetTriangles(subMeshTriangle.Value, i++);
            }
        }
        else
        {
            negativePartMesh = new Mesh();
            //if (negativePartVertices.Count == 0) return;
            negativePartMesh.SetVertices(negativePartVertices);
            negativePartMesh.SetNormals(negativePartNormals);
            negativePartMesh.SetUVs(0, negativePartUvs); //UV0(ͨ��0): ��Ҫ���ڻ�������ӳ�䣨diffuse texture, albedo�ȣ�,UV1(ͨ��1): ͨ�����ڹ�����ͼ��lightmap��,UV2(ͨ��2): �����ڶ����Ч����ϸ����ͼ
            negativePartMesh.SetUVs(1, negativePartUvs);

            negativePartMesh.subMeshCount = negativePartSubMeshTriangles.Count;
            int i = 0;
            foreach (KeyValuePair<int, List<int>> subMeshTriangle in negativePartSubMeshTriangles)
            {
                negativePartMesh.SetTriangles(subMeshTriangle.Value, i++);
            }
        }
    }
    private static void AddSubMeshTriangleInfo(bool isPositivePart, int subMeshIndex, List<string> ids, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv)
    {
        if (isPositivePart)
        {
            int newVertexIndex = positivePartVertices.Count;
            List<int> verticesIndexs = new List<int> { 0, 0, 0 };
            for (int i = 0, j = 0; i < 3; i++, j++)
            {
                if (positivePartVerticesID2Inndexs.ContainsKey(ids[i]))
                {
                    verticesIndexs[i] = positivePartVerticesID2Inndexs[ids[i]];//�������е�
                    vertices.RemoveAt(j);
                    normals.RemoveAt(j);
                    uv.RemoveAt(j);
                    j--;
                }
                else
                {
                    verticesIndexs[i] = newVertexIndex++;//�����µ�
                    positivePartVerticesID2Inndexs[ids[i]] = verticesIndexs[i];
                }
            }
            if (positivePartSubMeshTriangles.ContainsKey(subMeshIndex))
                positivePartSubMeshTriangles[subMeshIndex].AddRange(verticesIndexs);
            else
                positivePartSubMeshTriangles[subMeshIndex] = verticesIndexs;

            positivePartVertices.AddRange(vertices);
            positivePartNormals.AddRange(normals);
            positivePartUvs.AddRange(uv);
        }
        else
        {
            int newVertexIndex = negativePartVertices.Count;
            List<int> verticesIndexs = new List<int> { 0, 0, 0 };
            for (int i = 0, j = 0; i < 3; i++, j++)
            {
                if (negativePartVerticesID2Inndexs.ContainsKey(ids[i]))
                {
                    verticesIndexs[i] = negativePartVerticesID2Inndexs[ids[i]];//�������е�
                    vertices.RemoveAt(j);
                    normals.RemoveAt(j);
                    uv.RemoveAt(j);
                    j--;
                }
                else
                {
                    verticesIndexs[i] = newVertexIndex++;//�����µ�
                    negativePartVerticesID2Inndexs[ids[i]] = verticesIndexs[i];
                }
            }
            if (negativePartSubMeshTriangles.ContainsKey(subMeshIndex))
                negativePartSubMeshTriangles[subMeshIndex].AddRange(verticesIndexs);
            else
                negativePartSubMeshTriangles[subMeshIndex] = verticesIndexs;

            negativePartVertices.AddRange(vertices);
            negativePartNormals.AddRange(normals);
            negativePartUvs.AddRange(uv);
        }
    }
}

