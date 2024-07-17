using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfAssemblyExperimentsManager : MonoBehaviour
{
    public string sceneName = "self-assembly";
    public string dataPath = "C:/Users/pitte/Documents/Repositories/self_assembly_music/data_analysis/data/";
    public int[] agentsCount;
    public int[] joinTimes;
    public int samplesCount = 30;
    public int iterations = 30000;

    public static SelfAssemblyExperimentsManager Instance { get; private set; }

    public bool IsFinished { get; set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
        //Test
        //var currentFolder = "size-" + 0 + "-join-" + 1 + "/";
        //System.IO.Directory.CreateDirectory(dataPath + currentFolder);
        //System.IO.File.WriteAllText(dataPath + currentFolder  + "SYNC-STRUCT-2024_04_29_16_28_13-N-50-ls-1-hs-1-wt-1-jt-5-jr-2-js-6-rb-2-f-1.csv", "tessss");
        //Quit();
    }

    private string _currentFolder = "";
    private int _currentAgentCount = -1;
    private float _currentJoinTime = -1f;

    public void ConfigureController(SelfAssemblyController controller) {
        controller.agentsSize = _currentAgentCount;
        controller.PlaySound = false;
        controller.OnlySimulate = true;
        controller.iterations = iterations;
        controller.minAgentSpeed = 1f;
        controller.maxAgentSpeed = 1f;
        controller.Frequency = 1f;
        controller.maxJoinTime = _currentJoinTime;

        var dataCollection = controller.GetComponent<SelfAssemblyDataCollection>();
        dataCollection.saveData = true;
        dataCollection.dataPath = dataPath + _currentFolder;
    }   

    IEnumerator Start() {
        foreach (var agentCount in agentsCount) {
            _currentAgentCount = agentCount;
            foreach (var joinTime in joinTimes) {
                _currentJoinTime = joinTime;
                //create a folder in datapath
                _currentFolder = "size-" + _currentAgentCount + "-join-" + joinTime + "/";
                System.IO.Directory.CreateDirectory(dataPath + _currentFolder);
                for (int i = 0; i < samplesCount; i++) {
                    IsFinished = false;
                    //Load addtive scene
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                    //wait until experiment is finished
                    while (!IsFinished) {
                        yield return null;
                    }
                    //Unload scene
                    var unloadOp = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
                    //wait until scene is unloaded
                    while (!unloadOp.isDone) {
                        yield return null;
                    }
                    //wait 1 second just in case files have identical timestamps
                    yield return new WaitForSeconds(1f);
                }
            }            
        }  
        Quit();
    }

    public static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    UnityEngine.Application.Quit();
#endif
    }
}
