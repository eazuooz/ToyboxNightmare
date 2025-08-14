//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework.Entity;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public class DefaultEntityHelper : EntityHelperBase
    {
        private ResourceComponent m_ResourceComponent = null;

        public override object InstantiateEntity(object entityAsset)
        {
            return Instantiate((Object)entityAsset);
        }

        public override IEntity CreateEntity(object entityInstance, IEntityGroup entityGroup, object userData)
        {
            GameObject gameObject = entityInstance as GameObject;
            if (gameObject == null)
            {
                Log.Error("Entity instance is invalid.");
                return null;
            }

            Transform transform = gameObject.transform;
            transform.SetParent(((MonoBehaviour)entityGroup.Helper).transform);

            return gameObject.GetOrAddComponent<Entity>();
        }

        public override void ReleaseEntity(object entityAsset, object entityInstance)
        {
            m_ResourceComponent.UnloadAsset(entityAsset);
            Destroy((Object)entityInstance);
        }

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return;
            }
        }
    }
}
