# Self-Assembly and Synchronization: Crafting Music with Multi-Agent Embodied Oscillators

The extended version of the paper can be found in this repository [here](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/ACSOS2024_Paper___Self_Assembly_Music_EXTENDED_VERSION.pdf). The extended version provides a detailed description of the self-assembly algorithm and additional results and discussion.

## 1. Source Code

This repository is a [Unity](https://unity.com/) project (editor version 2022.3.13f1, look at the [Unity Archive](https://unity.com/releases/editor/archive)) where the self-assembly algorithm and music mapping is implemented. As we are using [Csound](https://csound.com/) through the wrapper [CsoundUnity](https://github.com/rorywalsh/CsoundUnity) for sound synthesis, the project currently can be run only in Windows and MacOS. Originally it was implemented in a Windows machine. Linux users can still use this project but they have to replace the sound generation mechanism since CsoundUnity does not provide binaries for this OS, or just use the project as per their own needs.

The main scene where the simulation is running is called [self-assembly.unity](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/Assets/Scenes/Self-AssemblyExperiments.unity). It has a default configuration that allows you to play the scene and run one session.

In general, you would need a fair understanding of Unity and the c# programming language to go through the code. We recommend to start looking at the scripts in the folder [`Agent Behaviour`](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/tree/main/Assets/Scripts/Agent%20Behaviour) where the self-assembly algorithm is implemented, especially consider the file [`SelfAssemblyAgent.cs`](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/Assets/Scripts/Agent%20Behaviour/SelfAssemblyAgent.cs) which has the finite state machine.

## 2. Data Collection and Analysis

Even though without a deeper understanding of Unity, it is possible to collect data to perform additional experiments and analysis. Thus, we indicate below how to do this by describing the collected data, how to perform the collection, and how to analyze it using Python (through Jupiter notebooks) to obtain the results in the paper using a previous dataset.

## 2.2. Data Description

One self-assembly session produce two `csv` files. One starts with the word `SYNC` and the other with `STRUCT`. Both follow the next naming convention considering that values go where it is indicated by `[]`:

`SYNC-[year]_[month]_[day]_[hour]_[minute]_[second]-N-[number of agents]-ls-[lowest speed]-hs-[highest speed]-wt-[wandering time]-jt-[join time]-jr-[join radius factor]-js-[number of slots]-rb-[boundary radius]-f-[oscillator frequency].csv`

The  `STRUCT` file follows the same convention but it starts with `STRUCT`.

For example, in one session recorded on May 5th, 2024; we have the following two files:

`SYNC-2024_05_05_10_46_09-N-50-ls-1-hs-1-wt-1-jt-5-jr-2-js-6-rb-2-f-1.csv`

`STRUCT-2024_05_05_10_46_09-N-50-ls-1-hs-1-wt-1-jt-5-jr-2-js-6-rb-2-f-1.csv`

It reflects the configuration for the session that can be changed in the script `SelfAssemblyController` in the Unity Editor as shown below.

![SelfAssemblyController](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/docs/selfAssemblyController.png)

The content for each file is explained as follows:

* **`SYNC` csv file:** It has two columns: `id` and `time`. The `id` variable corresponds to an agent's id and `time` is the timestamp in seconds in which the internal oscillator of an agent fires. It is useful to identify whether agents are synchronized with each other in an particular point in time.

* **`STRUCT` csv file:** It contains the following columns: `time`, `agent_ref`, `event`, `N`, `structure`. This file records the structures that are formed along time when a *'join'* or *'detach'* event happens, each line is a structure in an specific point in time. The column `time` is the timestamp in seconds in which one of these events happened, `agent_ref` is the agent that triggered the event represented by its ID, `event` is 0 when it was a *'join'* event or 1 when *'detach'*, `N` is the size of the structure of the current line in the file given by the number of agents that compose it, and `structure` is how the structure is composed, which is explained considering the next example:

    | time | agent_ref | event | N | structure |
    | --- | --- | --- | --- | --- |
    | 1.239999 | 15 | 0 | 1 | [1(0-0-0-0-0-0)]  | 
    | ... | ... | ... | ... | ... |
    | 1.239999 | 15 | 0 | 2 | [15(29-0-0-0-0-0)29(15-0-0-0-0-0)]  | 
    | ... | ... | ... | ... | ... |
    | 3.679997 | 28 | 0 | 3 | [28(41-0-0-0-0-0)41(46-28-0-0-0-0)46(41-0-0-0-0-0)]  | 
    | ... | ... | ... | ... | ... |
    | 21.4804 | 48 | 1 | 2 | [18(0-47-0-0-0-0)47(18-0-0-0-0-0)] | 
    | ... | ... | ... | ... | ... |


    The example above correspond to a session where 50 agents are involved. At time 1.239999, the agent with ID=15 trigger a *'join'* (0) event. Before this event we have 50 structures of size 1, meaning every agent was consider a structure; but at 1.239999, agent 15 joined to another agent, specifically to agent 29. We can see this in the file by noticing that we have 49 lines with the same timestamp 1.239999 denoting one structure of size 1 except for one of size 2. In that case, in the `structure` field we can identify how a structure is composed using the format `[agent_id1(slot_0-slot_1-slot_2-slot_3-slot_4-slot_5)agent_id2(slot_0-slot_1-slot_2-slot_3-slot_4-slot_5)...agent_idN(slot_0-slot_1-slot_2-slot_3-slot_4-slot_5)]` for the N agents that integrate that structure; that is, the agents in the structure are listed together with their slots, which are filled with the agentID connected to it or zero otherwise. In our example for the first structure of size 2, agent 15 and 29 are joined, 29 is in the slot 0  from 15 and 15 is in the slot 0 from 29. We can notice this pattern in the other lines.

## 2.3. Data Collection

### 2.3.1. Audio Recording

We captured a session from the Unity Editor with a third-party application for screen recording to obtain the video mentioned in the paper ([https://www.youtube.com/watch?v=GplsQD09y7Q](https://www.youtube.com/watch?v=GplsQD09y7Q)), then we separated the audio to analyze it later. You can apply the same procedure or capture the audio directly using other methods.

### 2.3.2. Data from an individual session

In the `self-assembly` scene from the Unity project, look for the `SelfAssemblyController` game object where the `SelfAssemblyController` script is attached (as shown in the image above in 2.2), under this script, there is another one called `SelfAssemblyDataCollection` that looks like the one below:

![selfAssemblyController-data](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/docs/selfAssemblyController-data.png)

This is the script that allows data to be collected and recorded in the files `SYNC` and `STRUCT` described previously for a session. You have to specify the absolute path where you want to save the files in the field `Data Path`, which now has a default value `C:/data/`. Also, to actually save the data, you have to ensure that the checkbox `Save Data` is activated.

For an individual session, there are two ways to record data:

* **Manual Recording**: In this case, you just play the scene and, when you press the key `S`, the two files will be saved considering the data collected from the beginning to the moment you press the key. If you press more times later, you will have new files with different timestamp in the file name, denoting the time in which `S` was pressed.

* **Automatic Recording**: If you just need to record data faster without experiencing the actual passage of time and record automatically after certain period, you can perform te following procedure:

    1. In the script `SelfAssemblyController` activate the checkbox `Only Simulate`, then specify how many iterations you want to simulate in the field `Iterations`. Considering that the default delta time for the Unity project for physics is 0.01 seconds, you can calculate the iterations you need based on how much time you want to simulate. To obtain this, you have to divide the seconds you need by 0.01; for instance, if you want to simulate 5 minutes (300 seconds) you would need 300/0.01 = 30000 iterations.
    2. In the same script `SelfAssemblyController` deactivate the checkbox `Play Sound` so that oscillators do not trigger any sound while in simulation mode.
    3. Play the scene and let the simulation finishes. You will notice that the scene stays static for some time (which is faster that the actual time from the iterations) since the session is running at the clock speed of CPU. When it stops, you will have the files in the specified path in the `SelfAssemblyDataCollection` script.

### 2.3.3. Data from multiple sessions

To record several trials which might involve changing parameters, we implemented an automatic way to run and capture multiple sessions. In the Unity project look for the scene `Self-AssemblyExperiments`. Here you can find only two game objects as examples, one of them is deactivated. Basically, these game objects contains a script called `SelfAssemblyExperimentsManager`, for instance, in the image below we have a screen capture of the script for the game object `Test-agents-50-jointime-5-50`.

![selfAssemblyExperimentsManager](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/docs/selfAssemblyExperimentsManager.png)

The fields for this script are described as follows:

| Parameter | Description |
| --- | --- |
| `Scene Name` | The unity scene where the self-assembly session takes place, which is now `self-assembly` | 
| `Data Path` | This is the path to a folder where all the data from the multiple sessions (folders and files) will be saved. If this directory does not exists, then the application will create it. |
| `Agents Count` | This is an array of integer numbers where we can specify a set of agents' sizes. For instance, if we add three elements (5, 20, 50) we will have sessions running for 5, 20 and 50 number of agents. |
| `Join Times` | This is another array for different values of Tjmax in seconds. If, for example, we have (1, 5, 25) we will have different sessions running with Tjmax of 1, 5, and 25 seconds |
| `Samples Count` | Here we can set the number of trials that we want per set of parameters. For example, if we have `Agents Count` = (10, 20, 30), `Join Times` = (1, 5), and `Samples Count` = 10, first we will have 6 types of sessions considering the combination of `Agents Count` and `Join Times`; then, we have 10 sessions per type of session, which means that we are running 60 sessions; that is, executing the scene `self-assembly` 60 times. |
| `Iterations` | The number of iterations per session, as explained in the previous section. |

As we are dealing with several sessions, every run is configured in simulation mode automatically, as indicated above in **Automatic Recording**. Moreover, after running multiple sessions, within the `Data Path` you will find several folders depending of the given settings. For instance, considering our previous example where `Agents Count` = (10, 20, 30), `Join Times` = (1, 5), and `Samples Count` = 10; you will find folders named as  `size-[agents' size]-join-[join time]` (eg. size-10-join-1, size-20-join-1, size-30-join-1, size-10-join-5, size-20-join-5, size-30-join-5), inside each folder there will be 20 .csv files (as `Samples Count` = 10, then 10 `SYNC` and 10 `STRUCT`).

By default, the `minAgentSpeed` and `minAgentSpeed` are automatically set to 1, and the oscillator `Frequency` also to 1. Other parameters can be changed in the `self-assembly` scene as indicated previously. If you want to control these or other additional parameters, you can modify the `SelfAssemblyExperimentsManager.cs` script accordingly.

Another important point is having only one game object active in the `Self-AssemblyExperiments` scene. We have two configuration examples as game objects there, you can try new ones but always remembering to deactivate the others before playing the scene.

## 2.4. Data Analysis

We have two Jupiter notebooks in the folder [`data_analysis`](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/tree/main/data_analysis). 

The notebook [`self-assembly-data-analysis-sync.ipynb`](self-assembly-data-analysis-sync.ipynb) plots the waveform and the spectrogram for a sound recording, and also analyzes `SYNC` files. The notebook [self-assembly-data-analysis-structures.ipynb](https://github.com/pedro-lucas-bravo/self_assembly_sync_music/blob/main/data_analysis/self-assembly-data-analysis-structures.ipynb) deals with the `STRUCT` files.

You can try this Python notebooks with data previously collected that you can download from [here](https://zenodo.org/doi/10.5281/zenodo.12805724). This is the data used in the paper.

Details are found in the corresponding notebooks and you can modify them according to your needs and considering how data is structured, which is mostly explained in the previous sections above.

## License

This software is released under the [GNU General Public License 3.0 license](https://www.gnu.org/licenses/gpl-3.0.en.html).

