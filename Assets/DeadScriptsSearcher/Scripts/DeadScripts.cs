﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Spiral.EditorTools.DeadScriptsSearcher.Localization;

#if UNITY_EDITOR
using UnityEditor;
namespace Spiral.EditorTools.DeadScriptsSearcher
{
    public class DeadScripts
    {
        public bool isDebugMode = true;

        public static DeadScripts instance { get; } = new DeadScripts() { isDebugMode = false };
        public List<ObjectID> deadOIDs  { get; private set; } = new List<ObjectID>();
        public List<ScriptGUID> deadGUIDs { get; private set; } = new List<ScriptGUID>();

        public bool sceneFileLoaded
        {
            get
            {
                if (sceneFile == null) return false;
                if (sceneFile.count == 0) return false;
                return true;
            }
        }

        public bool isDirty { get { return SceneManager.GetActiveScene().isDirty; } }

        private SceneFile sceneFile = null;

        // FUNCTIONALITY --------------------------------------------------------------------------
        public void SelectDeads()
        {
            ObjectID.Select(deadOIDs);
        }

        public void UpdateDeadList()
        {
            var objects = CoreFunctions.CollectScene().Transforms2GameObjects();
            int count = objects.Count;

            if (deadOIDs == null) deadOIDs = new List<ObjectID>();
            else if (deadOIDs.Count != 0) deadOIDs.Clear();

            for (int i = 0; i < count; i++)
            {
                GameObject go = objects[i];
                int missings = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missings == 0) continue;

                ObjectID objectID = new ObjectID(go, isDebugMode);
                deadOIDs.Add(objectID);
                EditorUtility.DisplayProgressBar(strProgressBar_SearchDeadObject,
                                                 strProgressBar_SearchingScene, 
                                                 i * 1f / count);
            }
            EditorUtility.ClearProgressBar();

            if (deadOIDs.Count == 0)
            {
                Debug.Log($"<color=green>Everything is okay :)</color>");
            }
        }

        public void SearchForDeads()
        {
            UpdateDeadList();
            sceneFile = new SceneFile();
            deadGUIDs = new List<ScriptGUID>();

            int count = deadOIDs.Count; // список deadOIDs сформирован функцией UpdateDeadList()
            for (int i = 0; i < count; i++)
            {
                EditorUtility.DisplayProgressBar(strProgressBar_SearchingSceneFile,
                                                 strProgressBar_InspectedObject + $"{i} / {count}", 
                                                 i * 1f / count);

                // инспектируемая учётка объекта
                ObjectID oid = deadOIDs[i];

                // идентфикаторы компонентов берутся напрямую из файла сцены
                // ulong gid компонента (равно как и объекта) - это уникальный идентификатор, позволяющий
                // найти компонент в файле сцены; два одинаковых компонента будут иметь одинаковый GUID,
                // но разный gid!
                List<ulong> componentGIDs = sceneFile.GetCGIDs(oid, isDebugMode);

                // если компонентные гиды не были взяты (объект находится в префабе или сцена была изменена)
                // если объект находится в префабе, его записи нет в файле сцены.
                if (componentGIDs == null) 
                {
                    if (isDebugMode) oid.DebugObjectNotFound();
                    continue;
                }

                // маловероятная ситуация, на практике у любого объекта есть как минимум один компонент
                // (это Transform); если мы падаем в этот случай, у нас, возможно, какие-то проблемы
                // с чтением файла сцены.
                if (componentGIDs.Count == 0) 
                {
                    if (isDebugMode) Debug.Log($"GIDs count of {oid.gameObject.name} is 0");
                    continue;
                }

                for (int g = 0; g < componentGIDs.Count; g++)
                {
                    ulong gid = componentGIDs[g];

                    // проверяет, что компонент в списке живых, и его рассматривать нет смысла
                    if (oid.liveIDs.Contains(gid)) continue; 

                    string guid = sceneFile.GetGUID(gid, isDebugMode);

                    // GUID не был найден в файле сцены. Это может произойти, если вы не сохранили сцену после изменений,
                    // если файл сцены повреждён. Также это происходит для скриптов, не имеющих поля m_Script в файле 
                    // сцены. Это скрипты вроде скрипта камеры, трансформа и т.п.
                    if (string.IsNullOrEmpty(guid))
                    {
                        if (isDebugMode) Debug.Log($"Script GID: {gid}; GUID not found");
                        continue;
                    }

                    // Если GUID найден, создаём учётку для скрипта
                    ScriptInstanceGID deadGID = new ScriptInstanceGID(gid, sceneFile);

                    // проверяем, есть ли уже учётка с таким GUID или нет 
                    // не путайте GID- и GUID- учётки: первая описывает конкретный экземпляр скрипта,
                    // а вторая - группу скриптов, имеющих одинаковый GUID
                    int guidIDX = deadGUIDs.FindIndex(x => x.guid == guid);
                    if (guidIDX >= 0) // добавляем мёртвый компонент и его объект к уже существующей учётке
                    {
                        deadGUIDs[guidIDX].oids.Add(oid);
                        deadGUIDs[guidIDX].gids.Add(deadGID);
                    }
                    else // создаём новую GUID-учётку
                    {
                        ScriptGUID deadGUID = new ScriptGUID(guid, true);
                        deadGUIDs.Add(deadGUID);
                        deadGUID.oids.Add(oid);
                        deadGUID.gids.Add(deadGID);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            if (isDebugMode)
            {
                for (int i = 0; i < deadGUIDs.Count; i++)
                {
                    Debug.Log($"Dead GUID <b>#{i}</b>: <i><color=red>{deadGUIDs[i]}</color></i> " +
                              $"(Scripts Broken: {deadGUIDs[i].oids.Count})");
                }
            }
        }
    }
}
#endif