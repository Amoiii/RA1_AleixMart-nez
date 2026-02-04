using UnityEngine;
using System.Collections;

public class MyRobotController : MonoBehaviour
{
    [Header("1. Articulaciones")]
    public Transform joint_0_Base;
    public Transform joint_1_Shoulder;
    public Transform joint_2_Elbow;
    public Transform joint_3_Wrist;
    public Transform joint_4_MiniElbow;
    public Transform joint_5_GripperRotate;

    [Header("2. Referencias")]
    public Transform endEffectorTarget;
    public Transform gripPoint;

    [Header("3. Capas y Detección")]
    public LayerMask obstacleLayer;
    public LayerMask grabbableLayer;
    public LayerMask dropZoneLayer;

    [Header("4. Ajustes")]
    public float touchRadius = 0.3f;
    public float manualRotationSpeed = 50.0f;
    public float autoSpeed = 1.0f;
    public float evasionSpeed = 60.0f;
    public float recoverySpeed = 30.0f;

    public float baseMoveSpeed = 4.0f;

    public bool blockManualOnCollision = true;
    public bool isBusy { get; private set; } = false;
    public bool manualMode = true;
    private GameObject heldObject = null;

    // Ángulos FK 
    private float baseAngleY = 0f;
    private float shoulderAngleX = 0f;
    private float elbowAngleX = 0f;
    private float wristAngleY = 0f;
    private float miniElbowAngleX = 0f;
    private float gripperAngleY = 0f;

    void Awake()
    {
         ApplyAllRotations();
    }

    void Update()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) moveZ = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) moveZ = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) moveX = 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;

        if (moveX != 0 || moveZ != 0)
        {
            // Usamos MyVec3 para mover la base
            MyVec3 dir = new MyVec3(moveX, 0, moveZ);

            float mag = dir.Magnitude();
            if (mag > 1f) dir = dir / mag;

            MyVec3 moveAmount = dir * baseMoveSpeed * Time.deltaTime;
            transform.Translate(moveAmount.ToUnity(), Space.World);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { manualMode = true; StopAllCoroutines(); isBusy = false; Debug.Log("Modo MANUAL"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { manualMode = false; }
        if (Input.GetKeyDown(KeyCode.P)) StartCoroutine(ResetArm());

        if (manualMode && !isBusy)
        {
            ControlManual();
            HandleActionInput();
        }
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Space))
        {
            if (heldObject == null) TryGrabObject(); else ReleaseObject();
        }
    }

    public void MoveToTarget(Vector3 unityTargetPos)
    {
        MyVec3 target = MyVec3.FromUnity(unityTargetPos);
        StartCoroutine(MoveToTargetReactive(target));
    }

    private IEnumerator MoveToTargetReactive(MyVec3 targetPos)
    {
        isBusy = true;
        //Usamos nuestros ángulos internos
        
        float currentEvasionOffset = 0f;

        while (true)
        {
            MyVec3 currentPos = MyVec3.FromUnity(transform.position);
            MyVec3 dir = targetPos - currentPos;

            // Cálculos con MyMath
            float idealBase = MyMath.Atan2(dir.x, dir.z) * MyMath.Rad2Deg;
            float dist = MyVec3.Distance(currentPos, targetPos);

            // Heurística FK
            float idealShoulder = MyMath.Clamp(dist * 10f, 0, 50);
            float idealElbow = MyMath.Clamp(dist * 5f, 20, 90);

            // Detección Reactiva
            bool muroEnfrente = Physics.Linecast(endEffectorTarget.position, targetPos.ToUnity(), obstacleLayer);

            if (muroEnfrente)
            {
                currentEvasionOffset -= evasionSpeed * Time.deltaTime;
                Debug.DrawLine(endEffectorTarget.position, targetPos.ToUnity(), Color.red);
            }
            else
            {
                currentEvasionOffset += recoverySpeed * Time.deltaTime;
                Debug.DrawLine(endEffectorTarget.position, targetPos.ToUnity(), Color.green);
            }

            currentEvasionOffset = MyMath.Clamp(currentEvasionOffset, -100f, 0f);
            float finalShoulder = idealShoulder + currentEvasionOffset;

            // Interpolación
            float dt = Time.deltaTime * autoSpeed;
            baseAngleY = MyMath.LerpAngle(baseAngleY, idealBase, dt);
            shoulderAngleX = MyMath.LerpAngle(shoulderAngleX, finalShoulder, dt * 2f);
            elbowAngleX = MyMath.LerpAngle(elbowAngleX, idealElbow, dt);

            ApplyAllRotations();

            if (MyMath.Abs(baseAngleY - idealBase) < 1f &&
                MyMath.Abs(shoulderAngleX - finalShoulder) < 2f &&
                !muroEnfrente)
            {
                break;
            }
            yield return null;
        }
        isBusy = false;
    }

    public IEnumerator MoveToPose(float[] target, float duration)
    {
        float t = 0;
        float[] start = { baseAngleY, shoulderAngleX, elbowAngleX, wristAngleY, miniElbowAngleX, gripperAngleY };

        while (t < 1)
        {
            t += Time.deltaTime * 2.0f / duration;
            float k = t * t * (3f - 2f * t);

            baseAngleY = MyMath.LerpAngle(start[0], target[0], k);
            shoulderAngleX = MyMath.LerpAngle(start[1], target[1], k);
            elbowAngleX = MyMath.LerpAngle(start[2], target[2], k);
            wristAngleY = MyMath.LerpAngle(start[3], target[3], k);
            miniElbowAngleX = MyMath.LerpAngle(start[4], target[4], k);
            gripperAngleY = MyMath.LerpAngle(start[5], target[5], k);

            ApplyAllRotations();
            yield return null;
        }
        ApplyAllRotations();
    }

    public IEnumerator ResetArm()
    {
        float[] home = { 0, 0, 0, 0, 0, 0 };
        yield return StartCoroutine(MoveToPose(home, 1.0f));
    }

  

    //uso de la propia librerias para la rotacion
    private void ApplyAllRotations()
    {
        // Convertimos nuestros ángulos a Cuaterniones usando MyMath.Euler
        
        if (joint_0_Base) joint_0_Base.localRotation = MyMath.Euler(0, baseAngleY, 0).ToUnity();
        if (joint_1_Shoulder) joint_1_Shoulder.localRotation = MyMath.Euler(shoulderAngleX, 0, 0).ToUnity();
        if (joint_2_Elbow) joint_2_Elbow.localRotation = MyMath.Euler(elbowAngleX, 0, 0).ToUnity();
        if (joint_3_Wrist) joint_3_Wrist.localRotation = MyMath.Euler(0, wristAngleY, 0).ToUnity();
        if (joint_4_MiniElbow) joint_4_MiniElbow.localRotation = MyMath.Euler(miniElbowAngleX, 0, 0).ToUnity();
        if (joint_5_GripperRotate) joint_5_GripperRotate.localRotation = MyMath.Euler(0, gripperAngleY, 0).ToUnity();
    }

    private void TryGrabObject()
    {
        Collider[] hits = Physics.OverlapSphere(endEffectorTarget.position, touchRadius, grabbableLayer);
        if (hits.Length > 0) ForceGrab(hits[0].gameObject);
    }

    public void ForceGrab(GameObject obj)
    {
        heldObject = obj;
        var rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        obj.transform.SetParent(gripPoint);
        obj.transform.localPosition = Vector3.zero;

        // Rotación manual con MyMath
        obj.transform.localRotation = MyMath.Euler(90, 0, 0).ToUnity();
    }

    public void ReleaseObject()
    {
        if (!heldObject) return;
        var rb = heldObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;
        heldObject.transform.SetParent(null);
        heldObject = null;
    }

    private void ControlManual()
    {
        float dt = manualRotationSpeed * Time.deltaTime;
        float b = baseAngleY, s = shoulderAngleX, e = elbowAngleX;
        float w = wristAngleY, m = miniElbowAngleX, g = gripperAngleY;

        if (Input.GetKey(KeyCode.A)) b -= dt;
        if (Input.GetKey(KeyCode.D)) b += dt;
        if (Input.GetKey(KeyCode.W)) s -= dt;
        if (Input.GetKey(KeyCode.S)) s += dt;
        if (Input.GetKey(KeyCode.Q)) e += dt;
        if (Input.GetKey(KeyCode.E)) e -= dt;
        if (Input.GetKey(KeyCode.Z)) w -= dt;
        if (Input.GetKey(KeyCode.C)) w += dt;
        if (Input.GetKey(KeyCode.R)) m += dt;
        if (Input.GetKey(KeyCode.F)) m -= dt;
        if (Input.GetKey(KeyCode.T)) g += dt;
        if (Input.GetKey(KeyCode.Y)) g -= dt;

        s = MyMath.Clamp(s, -100, 100);
        e = MyMath.Clamp(e, -10, 160);

        TryApplyAnglesSafely(b, s, e, w, m, g);
    }

    private void TryApplyAnglesSafely(float b, float s, float e, float w, float m, float g)
    {
        float oldB = baseAngleY, oldS = shoulderAngleX, oldE = elbowAngleX;
        float oldW = wristAngleY, oldM = miniElbowAngleX, oldG = gripperAngleY;

        baseAngleY = b; shoulderAngleX = s; elbowAngleX = e;
        wristAngleY = w; miniElbowAngleX = m; gripperAngleY = g;

        ApplyAllRotations();

        // Revertir si hay colisión
        if (blockManualOnCollision && CheckCollisionInternal())
        {
            baseAngleY = oldB; shoulderAngleX = oldS; elbowAngleX = oldE;
            wristAngleY = oldW; miniElbowAngleX = oldM; gripperAngleY = oldG;
            ApplyAllRotations();
        }
    }

    private bool CheckCollisionInternal()
    {
        if (Hit(joint_0_Base, joint_1_Shoulder)) return true;
        if (Hit(joint_1_Shoulder, joint_2_Elbow)) return true;
        if (Hit(joint_2_Elbow, joint_3_Wrist)) return true;
        if (Physics.CheckSphere(endEffectorTarget.position, 0.15f, obstacleLayer)) return true;
        return false;
    }
    private bool Hit(Transform a, Transform b) => Physics.CheckCapsule(a.position, b.position, 0.1f, obstacleLayer);

    // Sensores
    public bool IsTouchingObject(GameObject obj)
    {
        Collider[] hits = Physics.OverlapSphere(endEffectorTarget.position, touchRadius, grabbableLayer);
        foreach (var hit in hits) if (hit.gameObject == obj) return true;
        return false;
    }

    public bool IsInDropZone() => Physics.CheckSphere(endEffectorTarget.position, touchRadius, dropZoneLayer);

    void OnDrawGizmos()
    {
        if (endEffectorTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(endEffectorTarget.position, touchRadius);
        }
    }
}