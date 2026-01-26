using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MyRobotController))]
public class RobotSequenceAnimator : MonoBehaviour
{
    public Transform targetCube;
    public Transform dropZone;

    private MyRobotController bot;
    private bool isSequenceRunning = false;

    // Altura de seguridad para aproximarse
    private float alturaHover = 0.5f;

    void Awake() => bot = GetComponent<MyRobotController>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isSequenceRunning)
        {
            StartCoroutine(PerformPreciseSequence());
        }
    }

    private IEnumerator PerformPreciseSequence()
    {
        isSequenceRunning = true;
        bot.manualMode = false;
        Debug.Log(" INICIO SECUENCIA ");


        // 1. Hover sobre el cubo
        Vector3 cuboHoverPos = targetCube.position + Vector3.up * alturaHover;
        bot.MoveToTarget(cuboHoverPos);
        while (bot.isBusy) yield return null;

        // 2. Descender
        bot.MoveToTarget(targetCube.position);
        while (bot.isBusy) yield return null;
        yield return new WaitForSeconds(0.5f);

        // 3. Verificar y Agarrar
        if (bot.IsTouchingObject(targetCube.gameObject))
        {
            Debug.Log("Contacto. Agarrando.");
            bot.ForceGrab(targetCube.gameObject);
        }
        else
        {

            bot.ForceGrab(targetCube.gameObject);
        }
        yield return new WaitForSeconds(0.5f);

        // 4. Subir a Hover 
        bot.MoveToTarget(cuboHoverPos);
        while (bot.isBusy) yield return null;


        // ===  INSPECCIÓN  ===
        float currentBase = bot.joint_0_Base.localEulerAngles.y;
        float[] poseInspect = { currentBase, -30f, 45f, 0f, 45f, 90f };
        yield return StartCoroutine(bot.MoveToPose(poseInspect, 2.0f));
        yield return new WaitForSeconds(0.5f);


        //  ENTREGA (Lógica Grúa) 
        // 1. Hover sobre DropZone
        Vector3 dropHoverPos = dropZone.position + Vector3.up * alturaHover;
        bot.MoveToTarget(dropHoverPos);
        while (bot.isBusy) yield return null;

        // 2. Descender
        bot.MoveToTarget(dropZone.position);
        while (bot.isBusy) yield return null;
        yield return new WaitForSeconds(0.5f);

        // 3. Verificar y Soltar
        if (bot.IsInDropZone())
        {

            bot.ReleaseObject();
        }
        else
        {

            bot.ReleaseObject();
        }
        yield return new WaitForSeconds(0.5f);

        // 4. Subir a Hover
        bot.MoveToTarget(dropHoverPos);
        while (bot.isBusy) yield return null;


        yield return StartCoroutine(bot.ResetArm());

        Debug.Log("FIN ");
        bot.manualMode = true;
        isSequenceRunning = false;
    }
}