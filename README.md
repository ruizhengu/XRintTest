# XRintTest

# Requirements

**Unity:** 6000.0.36f1 LTS

**Newtonsoft Json** for json file parsing

1. Open Unity Editor
2. Go to **Window** -> **Package Manager**
3. Click the **+** symbol, then **Install package by name**
4. Enter `com.unity.nuget.newtonsoft-json`, then click **Install**

**XR Interaction Toolkit **& **XR Interaction Simulator** 

1. Open Unity Editor
2. Go to **Window** -> **Package Manager**
3. Go to the tab **Unity Registry**
4. Find **XR Interaction Toolkit** in **Packages**
5. Install with version >= 3.1.1
6. Within the **XR Interaction Toolkit** window, open the **Samples** tab
7. Import **XR Device Simulator**

# Instructions

**Scene Configuration**

1. Add XR Interaction Simulator
   1. Go to `/Assets/Samples/XR Interaction Toolkit/3.1.x/XR Device Simulator/XRInteractionSimulator` of the **Project** tab in Unity Editor
   2. Place the prefab **XR Interaction Simulator.prefab** in the scene under test
2. Add controller collider for oracle automation
   1. Go to the **Right Controller** game object under the **XR Origin** game object in the scene under test
   2. Add component **Mesh Collider** to the **Right Controller** game object
   3. Tick **Convex** and **Is Trigger**

**Scene Parser (build XUI graph)**

1. Copy the .cs scripts in the `/XRintTest/scripts` folder to the `/Assets/Scripts` folder of your Unity project
2. Open your scene under test in the Unity Editor
3. Open the top tab **Tools**, click **Generate XUI Graph**

**Dynamic Explorer (InteractoBot)**

1. Go to the `/Assets/Scripts` of the **Project** tab in Unity Editor
2. Attach the script **XRIntTest.cs** or **RandomBaseline.cs** to the **XR Origin** game object
3. Tick either **XRIntTest.cs** or **RandomBaseline.cs**
4. Play the scene under test to begin the test session

# Resource for Future Work

**Deep Learning (Unity ML Agents)**

* [Reinforcement Deep Q Learning for playing a game in Unity](https://medium.com/ml2vec/reinforcement-deep-q-learning-for-playing-a-game-in-unity-d2577fb50a81)
* [An Introduction to Unity ML-Agents](https://huggingface.co/learn/deep-rl-course/unit5/introduction)
* [ML-Agents: Hummingbirds](https://learn.unity.com/course/ml-agents-hummingbirds)

**Unity Code Coverage**

**Link:** https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.2/manual/Quickstart.html

1. Open Unity Editor
2. Go to **Window** -> **Package Manager**
3. Click the **+** symbol, then **Install package by name**
4. Enter `com.unity.testtools.codecoverage`, then click **Install**
5. Go to **Window** -> **Analysis** -> **Code Coverage**
6. Select **Enable Code Coverage**

# XRBench3D

# VR Template

**Link:** https://docs.unity3d.com/Packages/com.unity.template.vr@9.1/manual/index.html

**Unity Editor Version:** 6000.0.45f1

**XRI Version:** 3.1.1

**Scene Under Test:** SampleScene

# XRI Start Assets

**Link:** https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/samples-starter-assets.html

**Unity Editor Version:** 6000.0.45f1

**XRI Version:** 3.1.1

**Scene Under Test:** Demo Scene

# XRI Examples

**Link:** https://github.com/Unity-Technologies/XR-Interaction-Toolkit-Examples

**Unity Editor Version:** 6000.0.45f1

**XRI Version:** 3.1.1

**Scene Under Test:** XRI_Examples_Main

# VR-Game-Jam-Template

**Link:** https://github.com/ValemVR/VR-Game-Jam-Template

**Unity Editor Version:** 6000.0.45f1

**XRI Version:** 3.1.1

**Scene Under Test:** 2 Game Scene

# XRI Starter Kit

**Link:** https://assetstore.unity.com/packages/tools/game-toolkits/xr-interaction-toolkit-starter-kit-170222

**Unity Editor Version:** 6000.0.45f1

**XRI Version:** 3.1.1

**Scene Under Test:** XRI Starter Kit

# VR Beginner: The Escape Room

**Link:** https://assetstore.unity.com/packages/templates/tutorials/vr-beginner-the-escape-room-163264?srsltid=AfmBOoqhZh5LdXmRYc4vJgmQA--Wj0uIJCpI6XjNpgN-xrgzVUgnCqWA

**Unity Editor Version:** 6000.0.45f1

**XRI Version:** 3.1.1

**Scene Under Test:** Prototype Scene, Escape Room