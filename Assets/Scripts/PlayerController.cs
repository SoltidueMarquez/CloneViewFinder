using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// 玩家控制器：处理玩家移动、相机控制等功能
/// </summary>
public class PlayerController : Monobehavior
{
    // 基础移动参数
    public float speed = 6.0f;                  // 移动速度
    public float mouseSensitivity = 100.0f;     // 鼠标灵敏度
    public Transform cameraHolder;              // 相机持有者，用于控制相机旋转
    public bool isMoving;                       // 玩家是否正在移动
    public Transform filmPos;                   // 胶片位置
    public Volume ppProfile;                    // 后处理配置文件
    
    // 私有变量
    private CharacterController characterController;  // 角色控制器组件
    private float verticalVelocity = 0.0f;          // 垂直速度
    private float gravity =-9.8f;                    // 重力值
    private float jumpHeight = 1.5f;                 // 跳跃高度
    private float xRotation = 0f;                    // X轴旋转角度
    bool isPlayerActive=false;                       // 玩家是否可控制
    LensDistortion lensDistortion;                   // 镜头畸变效果组件

    /// <summary>
    /// 初始化
    /// </summary>
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;    // 锁定鼠标到屏幕中央
        isPlayerActive = true;                       // 激活玩家控制
        ppProfile.sharedProfile.TryGet<LensDistortion>(out lensDistortion);
        lensDistortion.active = false;               // 初始化时关闭镜头畸变效果
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    void Update()
    {
        if (isPlayerActive)
        {
            LookAround();    // 处理视角转动
            Move();          // 处理移动
        }
    }

    /// <summary>
    /// 切换玩家控制状态
    /// </summary>
    public void ChangePlayerState(bool isActive) {
        isPlayerActive = isActive;
    }

    /// <summary>
    /// 处理视角转动
    /// </summary>
    void LookAround()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 处理垂直视角旋转（上下看）
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);   // 限制视角范围

        // 应用旋转
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);            // 水平旋转
    }

    /// <summary>
    /// 处理玩家移动
    /// </summary>
    void Move()
    {
        // 获取水平和垂直输入
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // 更新移动状态
        isMoving = moveX != 0 || moveZ != 0f;

        // 计算移动方向
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // 处理重力和跳跃
        if (characterController.isGrounded)
        {
            verticalVelocity = -2f;    // 基础下落速度
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);    // 跳跃公式
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;    // 应用重力
        }

        // 应用最终移动
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 将物体移动到胶片位置
    /// </summary>
    public void MoveIntoFilmPosition(Transform t)
    {
        StartCoroutine(MoveFilm(t));
    }

    /// <summary>
    /// 处理物体向胶片位置移动的协程
    /// </summary>
    IEnumerator MoveFilm(Transform t) 
    {
        t.GetComponent<Animator>().enabled = false;    // 暂时禁用动画器

        var direction = filmPos.position - t.position;
        var prevMag = direction.magnitude + 0.01f;

        // 平滑移动到目标位置
        while (Vector3.Distance(filmPos.position, t.position) > 0.01f) {
            var dir = filmPos.position - t.position;
            if (dir.magnitude < prevMag)
            {
                prevMag = dir.magnitude;
                t.position += direction * Time.deltaTime * 1.5f;
                t.rotation = Quaternion.RotateTowards(t.rotation, filmPos.rotation, 5);
                yield return null;
            }
            else 
            {
                t.position = filmPos.position;
                t.rotation = filmPos.rotation;
                yield return null;
            }
        }

        t.GetComponent<Animator>().enabled = true;    // 重新启用动画器
    }

    /// <summary>
    /// 触发器检测，用于结束游戏
    /// </summary>
    void OnTriggerEnter(Collider other) {
        if (other.gameObject.name.Contains("Ending"))
            EndGame();
    }

    /// <summary>
    /// 结束游戏，激活镜头畸变效果
    /// </summary>
    void EndGame() {
        if (!lensDistortion.active)
        {
            lensDistortion.active = true;
            StartCoroutine(Ending());
        }
    }

    /// <summary>
    /// 结束游戏时的渐变效果
    /// </summary>
    IEnumerator Ending() {
        float x = 0;
        while (x < 0.5f)
        {
            x+=Time.deltaTime / 6;
            lensDistortion.intensity.value = x;    // 逐渐增加镜头畸变强度
            yield return null;
        }
    }

    /// <summary>
    /// 组件禁用时重置镜头畸变效果
    /// </summary>
    void OnDisable() {
        lensDistortion.intensity.value = 0;
    }
}
