using UnityEngine;
using System.Collections;

public class MyRobotController : MonoBehaviour
{
    [Header("1. Articulaciones")]
    public Transform joint_0_Base;
    public Transform joint_1_Shoulder;
    public Transform joint_2_Elbow;
    public Transform joint_3_Wrist;
    public Transform gripPoint;

    [Header("2. Medidas del Robot (MÍDELAS BIEN)")]
    public float upperArmLength = 2.0f;
    public float forearmLength = 2.0f;

    [Header("3. Ajustes de Precisión")]
    public float baseMoveSpeed = 4.0f; // Velocidad de mover el robot (teclas)
    public float smoothTime = 0.2f;    // Tiempo que tarda en llegar (Menor = más rápido/seco, Mayor = más suave)
    public float maxSpeed = 200.0f;    // Velocidad máxima de rotación (grados/segundo)

    [Header("4. Capas")]
    public LayerMask obstacleLayer;
    public LayerMask grabbableLayer;
    public LayerMask dropZoneLayer;

    // Estado
    public bool isBusy { get; private set; } = false;
    public bool manualMode = true;
    private GameObject heldObject = null;

    // Variables internas para SmoothDamp (Velocidades actuales)
    private float v_Base, v_Shoulder, v_Elbow, v_Wrist;

    // Ángulos objetivo
    private float t_Base, t_Shoulder, t_Elbow, t_Wrist;

    // Ángulos actuales visuales
    private float c_Base, c_Shoulder, c_Elbow, c_Wrist;

    void Start()
    {
        // Inicializar con la rotación actual
        if (joint_0_Base) c_Base = joint_0_Base.localEulerAngles.y;
        if (joint_1_Shoulder) c_Shoulder = FixAngle(joint_1_Shoulder.localEulerAngles.x);
        if (joint_2_Elbow) c_Elbow = FixAngle(joint_2_Elbow.localEulerAngles.x);
    }

    void Update()
    {
        // 1. MOVIMIENTO DE LA BASE (WASD / Flechas)
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            Vector3 moveDir = new Vector3(h, 0, v).normalized;
            transform.Translate(moveDir * baseMoveSpeed * Time.deltaTime, Space.World);
        }

        // Inputs
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            manualMode = true;
            StopAllCoroutines();
            isBusy = false;
            Debug.Log("Modo MANUAL");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) manualMode = false;
    }

    // =========================================================
    // LÓGICA DE MOVIMIENTO GEOMÉTRICO (FK)
    // =========================================================

    public void MoveToTarget(Vector3 worldTargetPos)
    {
        StopAllCoroutines();
        StartCoroutine(RutinaMoveToTarget(worldTargetPos));
    }

    private IEnumerator RutinaMoveToTarget(Vector3 targetPos)
    {
        isBusy = true;
        float timeOut = 6.0f; // Tiempo límite

        while (timeOut > 0)
        {
            timeOut -= Time.deltaTime;

            // A. Detección de Obstáculos
            // Lanzamos rayo desde la "cabeza" del hombro
            Vector3 shoulderPos = joint_1_Shoulder.position;
            bool hayMuro = Physics.Linecast(shoulderPos, targetPos, obstacleLayer);
            Debug.DrawLine(shoulderPos, targetPos, hayMuro ? Color.red : Color.green);

            // B. Calcular Ángulos
            if (hayMuro)
            {
                // ESTRATEGIA DE EVASIÓN: Apuntar a un punto seguro arriba y adelante
                // Esto hace que el brazo se pliegue hacia arriba
                Vector3 safeSkyPoint = transform.position + Vector3.up * 5.0f + transform.forward * 2.0f;
                CalculateFK_HighPrecision(safeSkyPoint);

                // Forzamos una pose de "Grúa"
                t_Elbow = -120f; // Cerrar mucho el codo
                t_Wrist = 60f;   // Compensar muñeca
            }
            else
            {
                // IR AL TARGET
                CalculateFK_HighPrecision(targetPos);
            }

            // C. APLICAR MOVIMIENTO SUAVE (SmoothDamp)
            // Esto es lo que lo hace sentir "Fino"
            c_Base = Mathf.SmoothDampAngle(c_Base, t_Base, ref v_Base, smoothTime, maxSpeed);
            c_Shoulder = Mathf.SmoothDampAngle(c_Shoulder, t_Shoulder, ref v_Shoulder, smoothTime, maxSpeed);
            c_Elbow = Mathf.SmoothDampAngle(c_Elbow, t_Elbow, ref v_Elbow, smoothTime, maxSpeed);
            c_Wrist = Mathf.SmoothDampAngle(c_Wrist, t_Wrist, ref v_Wrist, smoothTime, maxSpeed);

            ApplyRotations();

            // D. Comprobar llegada (Más estricto ahora)
            float distError = Vector3.Distance(gripPoint.position, targetPos);
            float angleError = Mathf.Abs(Mathf.DeltaAngle(c_Base, t_Base));

            // Solo terminamos si estamos muy cerca Y la base casi ha terminado de girar Y no hay muro
            if (distError < 0.1f && angleError < 5.0f && !hayMuro)
            {
                break;
            }

            yield return null;
        }
        isBusy = false;
    }

    // ---------------------------------------------------------
    // CÁLCULO TRIGONOMÉTRICO ROBUSTO
    // ---------------------------------------------------------
    void CalculateFK_HighPrecision(Vector3 targetWorldPos)
    {
        // 1. Target en espacio Local
        Vector3 localT = transform.InverseTransformPoint(targetWorldPos);

        // 2. Ángulo BASE (Y)
        t_Base = Mathf.Atan2(localT.x, localT.z) * Mathf.Rad2Deg;

        // 3. Preparar Triángulo
        // Offset vertical desde el hombro
        float yOffset = localT.y - joint_1_Shoulder.localPosition.y;

        // Distancia horizontal (XZ plane)
        float r = Mathf.Sqrt(localT.x * localT.x + localT.z * localT.z);

        // Distancia Directa al objetivo (Hipotenusa)
        float dist = Mathf.Sqrt(r * r + yOffset * yOffset);

        // --- PROTECCIONES (Hacen que sea estable) ---
        float totalLen = upperArmLength + forearmLength;
        // A. No estirar al 100% (evita chasquidos): Máximo 99.9%
        dist = Mathf.Clamp(dist, 0.2f, totalLen * 0.999f);

        // 4. LEY DE COSENOS
        // c^2 = a^2 + b^2 - 2ab cos(C)
        float a = upperArmLength;
        float b = forearmLength;
        float c = dist;

        // Ángulo interno Codo (Alpha)
        float cosElbow = (a * a + b * b - c * c) / (2 * a * b);
        float angleElbowRad = Mathf.Acos(Mathf.Clamp(cosElbow, -1f, 1f));

        // Ángulo interno Hombro (Beta)
        float cosShoulder = (a * a + c * c - b * b) / (2 * a * c);
        float angleShoulderTri = Mathf.Acos(Mathf.Clamp(cosShoulder, -1f, 1f));

        // Ángulo de elevación del target
        float angleElevation = Mathf.Atan2(yOffset, r);

        // 5. RESULTADOS
        // Codo: Normalmente es negativo en Unity para doblar hacia "arriba/adentro"
        t_Elbow = -(180f - (angleElbowRad * Mathf.Rad2Deg));

        // Hombro: Elevación + ángulo del triángulo. 
        // El +90 depende de si tu brazo en (0,0,0) está horizontal o vertical. 
        // Ajusta este +90 si apunta mal.
        t_Shoulder = -(angleShoulderTri + angleElevation) * Mathf.Rad2Deg + 90;

        // 6. MUÑECA INTELIGENTE
        // Calculamos el ángulo global del antebrazo y le restamos rotación para que
        // la mano quede plana (horizonte).
        // AngleGlobalForearm = t_Shoulder + t_Elbow (aprox en 2D plano vertical)
        // Queremos que Wrist compense: Wrist = - (Shoulder + Elbow)
        t_Wrist = -(t_Shoulder + t_Elbow);
    }

    void ApplyRotations()
    {
        if (joint_0_Base) joint_0_Base.localRotation = Quaternion.Euler(0, c_Base, 0);
        if (joint_1_Shoulder) joint_1_Shoulder.localRotation = Quaternion.Euler(c_Shoulder, 0, 0);
        if (joint_2_Elbow) joint_2_Elbow.localRotation = Quaternion.Euler(c_Elbow, 0, 0);
        if (joint_3_Wrist) joint_3_Wrist.localRotation = Quaternion.Euler(c_Wrist, 0, 0);
    }

    // =========================================================
    // UTILIDADES
    // =========================================================

    public IEnumerator ResetArm()
    {
        t_Base = 0; t_Shoulder = 0; t_Elbow = 0; t_Wrist = 0;
        float timeOut = 2.0f;
        while (timeOut > 0)
        {
            timeOut -= Time.deltaTime;
            // Usamos la misma lógica suave para el reset
            c_Base = Mathf.SmoothDampAngle(c_Base, 0, ref v_Base, smoothTime);
            c_Shoulder = Mathf.SmoothDampAngle(c_Shoulder, 0, ref v_Shoulder, smoothTime);
            c_Elbow = Mathf.SmoothDampAngle(c_Elbow, 0, ref v_Elbow, smoothTime);
            c_Wrist = Mathf.SmoothDampAngle(c_Wrist, 0, ref v_Wrist, smoothTime);

            ApplyRotations();

            if (Mathf.Abs(c_Base) < 1f && Mathf.Abs(c_Shoulder) < 1f) break;
            yield return null;
        }
    }

    // Helper para ángulos
    float FixAngle(float a) => a > 180 ? a - 360 : a;

    // Sistema de Agarre
    public void ForceGrab(GameObject obj)
    {
        if (heldObject != null) return;
        heldObject = obj;
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        obj.transform.SetParent(gripPoint);
        obj.transform.localPosition = Vector3.zero;
        // Rotar el objeto 90 grados respecto a la mano para que se vea "llevado"
        obj.transform.localRotation = Quaternion.Euler(0, 90, 0);
    }

    public void ReleaseObject()
    {
        if (!heldObject) return;
        Rigidbody rb = heldObject.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero; // Evitar que salga disparado
        }
        heldObject.transform.SetParent(null);
        heldObject = null;
    }

    // Gizmos para debug visual
    void OnDrawGizmos()
    {
        if (joint_1_Shoulder)
        {
            // Dibujar el alcance máximo
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawWireSphere(joint_1_Shoulder.position, upperArmLength + forearmLength);
        }
    }
}