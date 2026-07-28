using System.Collections.Generic;
using System.Linq;
using NightCafe.Core;
using UnityEditor;
using UnityEngine;

namespace NightCafe.EditorTools
{
    public static partial class NightCafeSetup
    {
        const string TagManagerPath = "ProjectSettings/TagManager.asset";

        /// <summary>
        /// Creates the GDD 5.1 sorting layers in TagManager.asset. Unity exposes no public API
        /// for this, so the settings asset is edited through SerializedObject.
        /// Must run before any renderer is created, otherwise layer assignment silently falls back
        /// to Default.
        /// </summary>
        static void EnsureSortingLayers()
        {
            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath).FirstOrDefault();
            if (tagManagerAsset == null)
            {
                Debug.LogError($"[NightCafe] Could not load {TagManagerPath}; sorting layers not created.");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            if (layers == null)
            {
                Debug.LogError("[NightCafe] TagManager has no m_SortingLayers property.");
                return;
            }

            var existing = new Dictionary<string, SerializedProperty>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                existing[element.FindPropertyRelative("name").stringValue] = element;
            }

            int changed = 0;
            foreach (string name in SortingLayers.InOrder)
            {
                if (existing.TryGetValue(name, out SerializedProperty entry))
                {
                    // Repair entries written with an invalid id: zero collides with Default,
                    // which makes the layer unusable and the name lookup fail silently.
                    if (entry.FindPropertyRelative("uniqueID").intValue != 0)
                        continue;

                    entry.FindPropertyRelative("uniqueID").intValue = UniqueIdFor(name);
                    changed++;
                    continue;
                }

                layers.InsertArrayElementAtIndex(layers.arraySize);
                SerializedProperty added = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                added.FindPropertyRelative("name").stringValue = name;
                added.FindPropertyRelative("uniqueID").intValue = UniqueIdFor(name);
                added.FindPropertyRelative("locked").boolValue = false;
                changed++;
            }

            if (changed == 0)
            {
                Debug.Log("[NightCafe] Sorting layers already present.");
                return;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[NightCafe] Wrote {changed} sorting layer(s): {string.Join(", ", SortingLayers.InOrder)}");
        }

        /// <summary>
        /// FNV-1a over the name, masked to a positive 31-bit value. Unity stores the id as a
        /// signed int and rejects negatives (they land as 0, which collides with Default).
        /// </summary>
        static int UniqueIdFor(string name)
        {
            uint hash = 2166136261u;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            int id = (int)(hash & 0x7FFFFFFF);
            return id == 0 ? 1 : id;
        }

        /// <summary>
        /// Assigns sorting layer and order, failing loudly: a typo'd layer name otherwise
        /// silently renders the object on Default, behind everything.
        /// </summary>
        static void SetSorting(SpriteRenderer renderer, string layerName, int order)
        {
            renderer.sortingLayerName = layerName;
            renderer.sortingOrder = order;

            if (renderer.sortingLayerName != layerName)
                Debug.LogError($"[NightCafe] Sorting layer '{layerName}' did not register on {renderer.name}.");
        }

        static void SetSorting(Renderer renderer, string layerName, int order)
        {
            renderer.sortingLayerName = layerName;
            renderer.sortingOrder = order;

            if (renderer.sortingLayerName != layerName)
                Debug.LogError($"[NightCafe] Sorting layer '{layerName}' did not register on {renderer.name}.");
        }
    }
}
