﻿using System.Collections.Generic;

#if UNITY_EDITOR
namespace Spiral.EditorTools.DeadScriptsSearcher
{
    /// <summary>
    /// Учётная запись для одного GUID'a (в основном для мёртвого, см. DeadScripts.cs).
    /// Собирает в себе все объекты, содержащие компоненты с этим GUID, а также сами
    /// компоненты. 
    /// </summary>
    public class ScriptGUID
    {
        /// <summary>
        /// GUID, ассоциированный с компонентом.
        /// GUID определяет идентификатор типа компонента, т.е. два
        /// компонента одного типа будут иметь одинаковый GUID. Так, 
        /// зная GUID мёртвого компонента, мы можем узнать, что два 
        /// потерянных компонента принадлежат (или не принадлежат)
        /// одному и тому же пропавшему скрипту.
        /// </summary>
        public string guid { get; }

        /// <summary>
        /// Все мёртвые объекты со скриптом этого вида
        /// </summary>
        public List<ObjectID> oids { get; } = new List<ObjectID>();

        /// <summary>
        /// Все экземпляры компонент со скриптом этого вида
        /// </summary>
        public List<ScriptInstanceGID> gids { get; } = new List<ScriptInstanceGID>();

        /// <summary>
        /// GUID принадлежит мёртвому скрипту
        /// </summary>
        public bool isDead { get; }

        /// <summary>
        /// Флаг для EditorWindow
        /// </summary>
        public bool showInfo { get; set; } = false;

        public ScriptGUID(string guid, bool isDead)
        {
            this.isDead = isDead;
            this.guid = guid;
        }
    }
}
#endif