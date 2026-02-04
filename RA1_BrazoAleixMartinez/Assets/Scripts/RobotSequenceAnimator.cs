using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MyRobotController))]
public class RobotSequenceAnimator : MonoBehaviour
{
    public Transform targetCube;
    public Transform dropZone;

    private MyRobotController bot;
    private bool isSequenceRunning = false;

    // Altura de seguridad 
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

        // 4. Subir  objeto
        bot.MoveToTarget(cuboHoverPos);
        while (bot.isBusy) yield return null;


      
        float currentBase = bot.GetBaseAngle();

        // Girar el objeto 
        float[] poseGiroA = { currentBase, -30f, 45f, 0f, 45f, 90f };
        yield return StartCoroutine(bot.MoveToPose(poseGiroA, 1.0f));


        float[] poseGiroB = { currentBase, -30f, 45f, 0f, 45f, -90f };
        yield return StartCoroutine(bot.MoveToPose(poseGiroB, 1.5f));

        //  Dejarlo recto otra vez antes de viajar
        float[] poseRecta = { currentBase, -30f, 45f, 0f, 45f, 0f };
        yield return StartCoroutine(bot.MoveToPose(poseRecta, 0.5f));

        yield return new WaitForSeconds(0.2f);

        // 5. ENTREGA

        Vector3 dropHoverPos = dropZone.position + Vector3.up * alturaHover;
        bot.MoveToTarget(dropHoverPos);
        while (bot.isBusy) yield return null;

        // 6. Descender
        bot.MoveToTarget(dropZone.position);
        while (bot.isBusy) yield return null;
        yield return new WaitForSeconds(0.5f);

        // 7. Verificar y Soltar
        if (bot.IsInDropZone())
        {
            bot.ReleaseObject();
        }
        else
        {
            bot.ReleaseObject();
        }
        yield return new WaitForSeconds(0.5f);

        // 8. Subir a Hover
        bot.MoveToTarget(dropHoverPos);
        while (bot.isBusy) yield return null;


        yield return StartCoroutine(bot.ResetArm());


        bot.manualMode = true;
        isSequenceRunning = false;
    }
}