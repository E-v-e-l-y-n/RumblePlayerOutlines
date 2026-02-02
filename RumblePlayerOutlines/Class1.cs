using UnityEngine;
using MelonLoader;
using System;
using RumblePlayerOutlines;
using Il2CppRUMBLE.Players;

[assembly: MelonInfo(typeof(RumblePlayerOutlines.Class1), ModInfo.Name, ModInfo.Version, ModInfo.Author)]
[assembly: MelonGame(null, null)]

namespace RumblePlayerOutlines
{
    public static class ModInfo
    {
        public const string Name = "RumblePlayerOutlines";
        public const string Description = "adds outlines to other players";
        public const string Author = "Evelyn";
        public const string Version = "1.0.0";
    }
    public class Class1 : MelonMod
    {
        public static Class1 instance;
        public Shader unlitShader;

        public override void OnLateInitializeMelon()
        {
            instance = this;
            unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        public System.Collections.IEnumerator CreatePlayerOutline(PlayerController player)
        {
            MelonLogger.Msg($"trying to create outline on player {player.assignedPlayer.Data.GeneralData.PublicUsername}");
            yield return new WaitForSeconds(1);
            Transform visuals = player.transform.GetChild(1);
            SkinnedMeshRenderer originalRenderer = visuals.GetChild(0).GetComponent<SkinnedMeshRenderer>();

            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(visuals);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;

            SkinnedMeshRenderer outlineRenderer = outline.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = originalRenderer.sharedMesh;

            outlineRenderer.material = GameObject.Instantiate(originalRenderer.material);
            outlineRenderer.sharedMaterial = GameObject.Instantiate(originalRenderer.sharedMaterial); // only really results in 1 material and the other gets deleted, i cant bother finding which sets which

            Transform skelington = visuals.GetChild(1);


            Transform[] outlineBones = new Transform[originalRenderer.bones.Length];

            DuplicateAllChildrenRecursive(skelington.GetChild(0), originalRenderer.bones, ref outlineBones); // the note is on the method itself

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
                            outlineRenderer.bones[i].position = chestBone.position; // sorta hide the head in first person, kinda goofy and will probably fuck something else (like the shadow) up
                            break;
                        }
                    }
                }
            }

            MelonLogger.Msg($"successfully created an outline for player {player.assignedPlayer.Data.GeneralData.PublicUsername}");
        }

        public void DuplicateAllChildrenRecursive(Transform transform, Transform[] bonesTarget, ref Transform[] bones) // goes through every bone, duplicates it, annihilates the children and then sets the outline's bones to the new duplicate
        {
            if (!transform.name.Contains("_Outline"))
            {
                Transform duplicate = GameObject.Instantiate(transform, transform);
                AnnihilateChildren(duplicate);
                duplicate.localPosition = Vector3.zero;
                duplicate.localRotation = Quaternion.identity;
                duplicate.localScale = Vector3.one * 1.05f; // arbitrary number that i don't wanna make a config for...
                duplicate.name = transform.name + "_Outline";
                int index = IndexOf(bonesTarget, transform.name);
                if (index != -1)
                {
                    bones[index] = duplicate;
                }

                for (int i = 0; i < transform.childCount; i++)
                {
                    DuplicateAllChildrenRecursive(transform.GetChild(i), bonesTarget, ref bones);
                }
            }
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
    }
}
