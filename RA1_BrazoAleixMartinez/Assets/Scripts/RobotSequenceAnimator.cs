using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MyRobotController))]
public class RobotSequenceAnimator : MonoBehaviour
{
    public Transform targetCube;
    public Transform dropZone;

    private MyRobotController bot;
    private bool isSequenceRunning = false;
    private float alturaHover = 1.0f;

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
        Debug.Log("--- INICIO SECUENCIA ---");

        // 1. Hover sobre el cubo
        Vector3 cuboHoverPos = targetCube.position + Vector3.up * alturaHover;
        bot.MoveToTarget(cuboHoverPos);
        while (bot.isBusy) yield return null;

        // 2. Descender
        bot.MoveToTarget(targetCube.position);
        while (bot.isBusy) yield return null;
        yield return new WaitForSeconds(0.2f);

        // 3. Agarrar (Aquí es donde se rota el objeto automáticamente)
        bot.ForceGrab(targetCube.gameObject);
        yield return new WaitForSeconds(0.2f);

        // 4. Subir a Hover (Evasión)
        Vector3 travelPoint = bot.transform.position + Vector3.up * 3.0f + bot.transform.forward * 1.5f;
        bot.MoveToTarget(travelPoint);
        while (bot.isBusy) yield return null;

        // 5. Ir al Drop Zone
        bot.MoveToTarget(dropZone.position + Vector3.up * 0.5f);
        while (bot.isBusy) yield return null;

        // 6. Soltar
        bot.ReleaseObject();
        yield return new WaitForSeconds(0.5f);

        // 7. Reset
        yield return StartCoroutine(bot.ResetArm());

        Debug.Log("--- FIN ---");
        bot.manualMode = true;
        isSequenceRunning = false;
    }
}