using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Project.Scripts.Utils;

[Serializable]
public class DependencyStartup
{
    public string StepName = "";
    [TextArea(2,4)]
    public string StepAnnotation = "";
    public bool DontExecute = false;
    public bool DontWait = false;
    public float CheckDelay = 0.0f;
    public float WaitAfter = 0.0f;
    public List<ProjectMonoBehaviour> DependencyComponents = new List<ProjectMonoBehaviour>();
    public List<MonoBehaviour> SimpleComponents = new List<MonoBehaviour>();
    // startup timeout?
    // startup enable in case of fail?

    public ProjectMonoBehaviour GetNext()
    {
        nextComponent++;
        if (nextComponent >= DependencyComponents.Count)
            return null;
        else
            return DependencyComponents[nextComponent];
    }

    private int nextComponent = -1;

    public string name => StepName;
}

public class StartupScript : ProjectMonoBehaviour
{
    [Min(0.0f)] public float StartupDelay = 0.0f;
    [Min(-1)] public int RunUntilStep = -1;
    public List<DependencyStartup> Dependencies = new List<DependencyStartup>();

    private void Start()
    {
        string sourceLog = "StartupScript:Start";

        if (StaticAppSettings.IsEnvUWP)
            RunUntilStep = int.MaxValue;
        else if (RunUntilStep < 0)
            RunUntilStep = Dependencies.Count;

        StaticLogger.Info(sourceLog, $"running OnValidate...", logLayer: 0);
        OnValidate();
        StaticLogger.Info(sourceLog, $"beginning startup process... (found {Dependencies.Count} dependencies)", logLayer: 0);
        StartCoroutine(ORCOR_StartupProcedure());
    }

    private IEnumerator ORCOR_StartupProcedure()
    {
        string sourceLog = "StartupScript:ORCOR_StartupProcedure";
        if (StartupDelay > 0.0f)
            yield return new WaitForSecondsRealtime(StartupDelay);
        else
            yield return new WaitForEndOfFrame();

        int stepCount = -1;
        foreach (DependencyStartup dp in Dependencies)
        {
            stepCount++;
            StaticLogger.Info(sourceLog, $"startup step: {stepCount}", logLayer: 0);
            yield return BSCOR_WaiForDependency(dp);

            if (stepCount >= RunUntilStep)
                break;
        }

        StaticLogger.Info(sourceLog, $"Startup process end ({stepCount+1} steps performed)", logLayer: 3);
        Ready(disableComponent: true);
    }

    private IEnumerator BSCOR_WaiForDependency(DependencyStartup dp)
    {
        string sourceLog = "StartupScript:BSCOR_WaiForDependency";
        yield return null;
        if (dp.DontExecute) yield break;

        ProjectMonoBehaviour curPmb = dp.GetNext();
        if (curPmb == null) yield break;

        foreach (ProjectMonoBehaviour pmb in dp.DependencyComponents)
        {
            StaticLogger.Info(sourceLog, $"starting (ProjectMonoBehaviour) {pmb.name}", logLayer: 3);
            pmb.enabled = true;
        }
        foreach (MonoBehaviour mb in dp.SimpleComponents)
        {
            StaticLogger.Info(sourceLog, $"starting (MonoBehaviour) {mb.name}", logLayer: 3);
            mb.enabled = true;
        }
        
        while (!dp.DontWait)
        {
            if (dp.CheckDelay > 0.0f)
                yield return new WaitForSecondsRealtime(dp.CheckDelay);
            else
                yield return new WaitForEndOfFrame();

            if (!curPmb.IsReady)
            {
                StaticLogger.Info(sourceLog, $"not ready", logLayer: 3);
                continue;
            }
            StaticLogger.Info(sourceLog, $"component is ready; getting next", logLayer: 3);
            curPmb = dp.GetNext();
            if(curPmb == null)
            {
                StaticLogger.Info(sourceLog, $"no more components inside the list", logLayer: 3);
                break;
            }
            else StaticLogger.Info(sourceLog, $"another component", logLayer: 3);
        }

        if(dp.WaitAfter > 0.0f)
            yield return new WaitForSecondsRealtime(dp.WaitAfter);
    }

    [ExecuteInEditMode]
    private void OnValidate()
    {
        foreach(DependencyStartup dp in Dependencies)
        {
            foreach (ProjectMonoBehaviour pmb in dp.DependencyComponents)
            {
                try
                {
                    pmb.enabled = false;
                }
                catch (System.Exception)
                {
                    // https://www.youtube.com/watch?v=rlhRQiVeQPY
                }
            }
            foreach(MonoBehaviour mb in dp.SimpleComponents)
            {
                try
                {
                    mb.enabled = false;
                }
                catch (System.Exception)
                {
                    // https://www.youtube.com/watch?v=rlhRQiVeQPY
                }
            }
        }
    }
}
