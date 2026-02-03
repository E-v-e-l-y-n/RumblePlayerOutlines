using UnityEngine;
using MelonLoader;
using System;
using RumblePlayerOutlines;
using Il2CppRUMBLE.Players;
using RumbleModUI;
using Il2CppRUMBLE.Managers;

[assembly: MelonInfo(typeof(RumblePlayerOutlines.Class1), ModInfo.Name, ModInfo.Version, ModInfo.Author)]
[assembly: MelonGame(null, null)]

namespace RumblePlayerOutlines
{
    public static class ModInfo
    {
        public const string Name = "RumblePlayerOutlines";
        public const string Description = "adds outlines to other players";
        public const string Author = "Evelyn";
        public const string Version = "1.1.0";
    }

    public class Class1 : MelonMod
    {
        public static Class1 instance;
        public Shader unlitShader;

        private Mod RumblePlayerOutlines = new Mod();
        ModSetting<int> outlineSize;
        ModSetting<bool> outlineEnabled;

        public override void OnLateInitializeMelon()
        {
            instance = this;
            unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

            RumblePlayerOutlines.ModName = "RumblePlayerOutlines";
            RumblePlayerOutlines.ModVersion = ModInfo.Version;
            RumblePlayerOutlines.SetFolder("RumblePlayerOutlines");
            outlineEnabled = RumblePlayerOutlines.AddToList("Toggle outlines", true, 0, "Toggles the outlines on players", new Tags());
            outlineSize = RumblePlayerOutlines.AddToList("Outline Size", 5, "How big the outlines are", new Tags());
            RumblePlayerOutlines.GetFromFile();
            RumblePlayerOutlines.ModSaved += OnModUISaved;

            UI.instance.UI_Initialized += delegate { UI.instance.AddMod(RumblePlayerOutlines); };
        }

        public void OnModUISaved()
        {
            foreach (Player player in PlayerManager.instance.AllPlayers)
            {
                Transform visuals = FindChildByName(player.Controller.transform, "Visuals");
                Transform outline = FindChildByName(visuals, "Outline");

                outline.gameObject.SetActive((bool)outlineEnabled.Value);
                foreach (Transform bone in outline.GetComponent<SkinnedMeshRenderer>().bones)
                {
                    if (bone.localScale != Vector3.zero)
                    {
                        bone.localScale = Vector3.one * GetOutlineSize();
                    }
                }
            }
        }

        public System.Collections.IEnumerator CreatePlayerOutline(PlayerController player)
        {
            MelonLogger.Msg($"trying to create outline on player {player.assignedPlayer.Data.GeneralData.PublicUsername}");
            yield return new WaitForSeconds(1);
            Transform visuals = FindChildByName(player.transform, "Visuals"); // not sure if LCKViewport exists on other players so this is safer
            SkinnedMeshRenderer originalRenderer = FindChildByName(visuals, "Renderer").GetComponent<SkinnedMeshRenderer>(); // in case it moves from position 0

            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(visuals);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;

            SkinnedMeshRenderer outlineRenderer = outline.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = originalRenderer.sharedMesh;

            outlineRenderer.material = GameObject.Instantiate(originalRenderer.material);
            outlineRenderer.sharedMaterial = GameObject.Instantiate(originalRenderer.sharedMaterial); // only really results in 1 material and the other gets deleted, i cant bother finding which sets which

            Transform skelington = visuals.GetChild(1);

            Transform[] outlineBones = DuplicateBones(originalRenderer.bones);

            outlineRenderer.bones = outlineBones;
            outlineRenderer.rootBone = outlineBones[IndexOf(outlineBones, originalRenderer.rootBone.name + "_Outline")];

            outlineRenderer.material.shader = unlitShader;
            outlineRenderer.material.color = Color.black;
            outlineRenderer.material.SetFloat("_Cull", 1f);

            outlineRenderer.sharedMaterial.shader = unlitShader;
            outlineRenderer.sharedMaterial.color = Color.black;
            outlineRenderer.sharedMaterial.SetFloat("_Cull", 1f); // totally didnt steal this from structure outlines

            string[] namesToCheck = new string[] { "mouth", "face", "brow", "eye", "nose", "nazal", "head", "jaw", "lip", "hair", "tail" }; // keywords for bones to look for, if any are missing @ me
            if (player.controllerType == ControllerType.Local) // "delete" the head if it's the local player
            {
                Transform chestBone = outlineRenderer.bones[IndexOf(outlineRenderer.bones, "Bone_Chest_Outline")];
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                for (int i = 0; i < outlineRenderer.bones.Length; i++)
                {
                    string name = outlineRenderer.bones[i].name.ToLower();
                    foreach (string nameToCheck in namesToCheck)
                    {
                        if (name.Contains(nameToCheck))
                        {
                            outlineRenderer.bones[i].localScale = Vector3.zero;
                            outlineRenderer.bones[i].position = chestBone.position; // sorta hide the head in first person, doesnt work well with rockcam
                            break;
                        }
                    }
                }
            }

            MelonLogger.Msg($"successfully created an outline for player {player.assignedPlayer.Data.GeneralData.PublicUsername}");
        }

        public Transform[] DuplicateBones(Transform[] bonesTarget) // significantly better than the previous recursive function
        {
            Transform[] duplicateBones = new Transform[bonesTarget.Length];

            for (int i = 0; i < bonesTarget.Length; i++)
            {
                duplicateBones[i] = GameObject.Instantiate(bonesTarget[i], bonesTarget[i]);
                AnnihilateChildren(duplicateBones[i]);
                duplicateBones[i].localPosition = Vector3.zero;
                duplicateBones[i].localRotation = Quaternion.identity;
                duplicateBones[i].localScale = Vector3.one * GetOutlineSize();
                duplicateBones[i].name = bonesTarget[i].name + "_Outline";
            }

            return duplicateBones;
        }

        public int IndexOf(Transform[] array, string name) // istg if someone asks what this does
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].name == name) return i;
            }
            //MelonLogger.Error($"couldnt find the index of a transform with name {name}"); // produced unnecessary errors with objects that are children but arent a bone
            return -1;
        }

        public void AnnihilateChildren(Transform obj) // yeetus fetus
        {
            for (int i = obj.childCount - 1; i >= 0; i--)
                GameObject.Destroy(obj.GetChild(i).gameObject);
        }

        public Transform FindChildByName(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == name)
                    return parent.GetChild(i);
            }
            return null;
        }

        public float GetOutlineSize()
        {
            return (1 + ((int)outlineSize.Value / 100f));
        }
    }
}
