//------------------------------------------------------------
// Game Framework - MIT License
// Copyright © 2013–2021 Jiang Yin (EllanJiang)
// Modified © 2025 얌얌코딩
// Homepage: https://www.yamyamcoding.com/
// Feedback: mailto:eazuooz@gmail.com
//------------------------------------------------------------


using GameFramework;
using GameFramework.Entity;
using GameFramework.ObjectPool;
using GameFramework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Entity")]
    public sealed partial class EntityComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private IEntityManager m_EntityManager = null;
        private EventComponent m_EventComponent = null;

        private readonly List<IEntity> m_InternalEntityResults = new List<IEntity>();

        [SerializeField]
        private bool m_EnableShowEntityUpdateEvent = false;

        [SerializeField]
        private bool m_EnableShowEntityDependencyAssetEvent = false;

        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_EntityHelperTypeName = "UnityGameFramework.Runtime.DefaultEntityHelper";

        [SerializeField]
        private EntityHelperBase m_CustomEntityHelper = null;

        [SerializeField]
        private string m_EntityGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultEntityGroupHelper";

        [SerializeField]
        private EntityGroupHelperBase m_CustomEntityGroupHelper = null;

        [SerializeField]
        private EntityGroup[] m_EntityGroups = null;

        public int EntityCount
        {
            get
            {
                return m_EntityManager.EntityCount;
            }
        }

        public int EntityGroupCount
        {
            get
            {
                return m_EntityManager.EntityGroupCount;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_EntityManager = GameFrameworkEntry.GetModule<IEntityManager>();
            if (m_EntityManager == null)
            {
                Log.Fatal("Entity manager is invalid.");
                return;
            }

            m_EntityManager.ShowEntitySuccess += OnShowEntitySuccess;
            m_EntityManager.ShowEntityFailure += OnShowEntityFailure;

            if (m_EnableShowEntityUpdateEvent)
            {
                m_EntityManager.ShowEntityUpdate += OnShowEntityUpdate;
            }

            if (m_EnableShowEntityDependencyAssetEvent)
            {
                m_EntityManager.ShowEntityDependencyAsset += OnShowEntityDependencyAsset;
            }

            m_EntityManager.HideEntityComplete += OnHideEntityComplete;
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            m_EventComponent = GameEntry.GetComponent<EventComponent>();
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (baseComponent.EditorResourceMode)
            {
                m_EntityManager.SetResourceManager(baseComponent.EditorResourceHelper);
            }
            else
            {
                m_EntityManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
            }

            m_EntityManager.SetObjectPoolManager(GameFrameworkEntry.GetModule<IObjectPoolManager>());

            EntityHelperBase entityHelper = Helper.CreateHelper(m_EntityHelperTypeName, m_CustomEntityHelper);
            if (entityHelper == null)
            {
                Log.Error("Can not create entity helper.");
                return;
            }

            entityHelper.name = "Entity Helper";
            Transform transform = entityHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            m_EntityManager.SetEntityHelper(entityHelper);

            if (m_InstanceRoot == null)
            {
                m_InstanceRoot = new GameObject("Entity Instances").transform;
                m_InstanceRoot.SetParent(gameObject.transform);
                m_InstanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < m_EntityGroups.Length; i++)
            {
                if (!AddEntityGroup(m_EntityGroups[i].Name, m_EntityGroups[i].InstanceAutoReleaseInterval, m_EntityGroups[i].InstanceCapacity, m_EntityGroups[i].InstanceExpireTime, m_EntityGroups[i].InstancePriority))
                {
                    Log.Warning("Add entity group '{0}' failure.", m_EntityGroups[i].Name);
                    continue;
                }
            }
        }

        public bool HasEntityGroup(string entityGroupName)
        {
            return m_EntityManager.HasEntityGroup(entityGroupName);
        }

        public IEntityGroup GetEntityGroup(string entityGroupName)
        {
            return m_EntityManager.GetEntityGroup(entityGroupName);
        }

        public IEntityGroup[] GetAllEntityGroups()
        {
            return m_EntityManager.GetAllEntityGroups();
        }

        public void GetAllEntityGroups(List<IEntityGroup> results)
        {
            m_EntityManager.GetAllEntityGroups(results);
        }

        public bool AddEntityGroup(string entityGroupName, float instanceAutoReleaseInterval, int instanceCapacity, float instanceExpireTime, int instancePriority)
        {
            if (m_EntityManager.HasEntityGroup(entityGroupName))
            {
                return false;
            }

            EntityGroupHelperBase entityGroupHelper = Helper.CreateHelper(m_EntityGroupHelperTypeName, m_CustomEntityGroupHelper, EntityGroupCount);
            if (entityGroupHelper == null)
            {
                Log.Error("Can not create entity group helper.");
                return false;
            }

            entityGroupHelper.name = Utility.Text.Format("Entity Group - {0}", entityGroupName);
            Transform transform = entityGroupHelper.transform;
            transform.SetParent(m_InstanceRoot);
            transform.localScale = Vector3.one;

            return m_EntityManager.AddEntityGroup(entityGroupName, instanceAutoReleaseInterval, instanceCapacity, instanceExpireTime, instancePriority, entityGroupHelper);
        }

        public bool HasEntity(int entityId)
        {
            return m_EntityManager.HasEntity(entityId);
        }

        public bool HasEntity(string entityAssetName)
        {
            return m_EntityManager.HasEntity(entityAssetName);
        }

        public Entity GetEntity(int entityId)
        {
            return (Entity)m_EntityManager.GetEntity(entityId);
        }

        public Entity GetEntity(string entityAssetName)
        {
            return (Entity)m_EntityManager.GetEntity(entityAssetName);
        }

        public Entity[] GetEntities(string entityAssetName)
        {
            IEntity[] entities = m_EntityManager.GetEntities(entityAssetName);
            Entity[] entityImpls = new Entity[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = (Entity)entities[i];
            }

            return entityImpls;
        }

        public void GetEntities(string entityAssetName, List<Entity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetEntities(entityAssetName, m_InternalEntityResults);
            foreach (IEntity entity in m_InternalEntityResults)
            {
                results.Add((Entity)entity);
            }
        }

        public Entity[] GetAllLoadedEntities()
        {
            IEntity[] entities = m_EntityManager.GetAllLoadedEntities();
            Entity[] entityImpls = new Entity[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = (Entity)entities[i];
            }

            return entityImpls;
        }

        public void GetAllLoadedEntities(List<Entity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetAllLoadedEntities(m_InternalEntityResults);
            foreach (IEntity entity in m_InternalEntityResults)
            {
                results.Add((Entity)entity);
            }
        }

        public int[] GetAllLoadingEntityIds()
        {
            return m_EntityManager.GetAllLoadingEntityIds();
        }

        public void GetAllLoadingEntityIds(List<int> results)
        {
            m_EntityManager.GetAllLoadingEntityIds(results);
        }

        public bool IsLoadingEntity(int entityId)
        {
            return m_EntityManager.IsLoadingEntity(entityId);
        }

        public bool IsValidEntity(Entity entity)
        {
            return m_EntityManager.IsValidEntity(entity);
        }

        public void ShowEntity<T>(int entityId, string entityAssetName, string entityGroupName) where T : EntityLogic
        {
            ShowEntity(entityId, typeof(T), entityAssetName, entityGroupName, DefaultPriority, null);
        }

        public void ShowEntity(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName)
        {
            ShowEntity(entityId, entityLogicType, entityAssetName, entityGroupName, DefaultPriority, null);
        }

        public void ShowEntity<T>(int entityId, string entityAssetName, string entityGroupName, int priority) where T : EntityLogic
        {
            ShowEntity(entityId, typeof(T), entityAssetName, entityGroupName, priority, null);
        }

        public void ShowEntity(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, int priority)
        {
            ShowEntity(entityId, entityLogicType, entityAssetName, entityGroupName, priority, null);
        }

        public void ShowEntity<T>(int entityId, string entityAssetName, string entityGroupName, object userData) where T : EntityLogic
        {
            ShowEntity(entityId, typeof(T), entityAssetName, entityGroupName, DefaultPriority, userData);
        }

        public void ShowEntity(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, object userData)
        {
            ShowEntity(entityId, entityLogicType, entityAssetName, entityGroupName, DefaultPriority, userData);
        }

        public void ShowEntity<T>(int entityId, string entityAssetName, string entityGroupName, int priority, object userData) where T : EntityLogic
        {
            ShowEntity(entityId, typeof(T), entityAssetName, entityGroupName, priority, userData);
        }

        public void ShowEntity(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, int priority, object userData)
        {
            if (entityLogicType == null)
            {
                Log.Error("Entity type is invalid.");
                return;
            }

            m_EntityManager.ShowEntity(entityId, entityAssetName, entityGroupName, priority, ShowEntityInfo.Create(entityLogicType, userData));
        }

        public void HideEntity(int entityId)
        {
            m_EntityManager.HideEntity(entityId);
        }

        public void HideEntity(int entityId, object userData)
        {
            m_EntityManager.HideEntity(entityId, userData);
        }

        public void HideEntity(Entity entity)
        {
            m_EntityManager.HideEntity(entity);
        }

        public void HideEntity(Entity entity, object userData)
        {
            m_EntityManager.HideEntity(entity, userData);
        }

        public void HideAllLoadedEntities()
        {
            m_EntityManager.HideAllLoadedEntities();
        }

        public void HideAllLoadedEntities(object userData)
        {
            m_EntityManager.HideAllLoadedEntities(userData);
        }

        public void HideAllLoadingEntities()
        {
            m_EntityManager.HideAllLoadingEntities();
        }

        public Entity GetParentEntity(int childEntityId)
        {
            return (Entity)m_EntityManager.GetParentEntity(childEntityId);
        }

        public Entity GetParentEntity(Entity childEntity)
        {
            return (Entity)m_EntityManager.GetParentEntity(childEntity);
        }

        public int GetChildEntityCount(int parentEntityId)
        {
            return m_EntityManager.GetChildEntityCount(parentEntityId);
        }

        public Entity GetChildEntity(int parentEntityId)
        {
            return (Entity)m_EntityManager.GetChildEntity(parentEntityId);
        }

        public Entity GetChildEntity(IEntity parentEntity)
        {
            return (Entity)m_EntityManager.GetChildEntity(parentEntity);
        }

        public Entity[] GetChildEntities(int parentEntityId)
        {
            IEntity[] entities = m_EntityManager.GetChildEntities(parentEntityId);
            Entity[] entityImpls = new Entity[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = (Entity)entities[i];
            }

            return entityImpls;
        }

        public void GetChildEntities(int parentEntityId, List<IEntity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetChildEntities(parentEntityId, m_InternalEntityResults);
            foreach (IEntity entity in m_InternalEntityResults)
            {
                results.Add((Entity)entity);
            }
        }

        public Entity[] GetChildEntities(Entity parentEntity)
        {
            IEntity[] entities = m_EntityManager.GetChildEntities(parentEntity);
            Entity[] entityImpls = new Entity[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                entityImpls[i] = (Entity)entities[i];
            }

            return entityImpls;
        }

        public void GetChildEntities(IEntity parentEntity, List<IEntity> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            m_EntityManager.GetChildEntities(parentEntity, m_InternalEntityResults);
            foreach (IEntity entity in m_InternalEntityResults)
            {
                results.Add((Entity)entity);
            }
        }

        public void AttachEntity(int childEntityId, int parentEntityId)
        {
            AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), string.Empty, null);
        }

        public void AttachEntity(int childEntityId, Entity parentEntity)
        {
            AttachEntity(GetEntity(childEntityId), parentEntity, string.Empty, null);
        }

        public void AttachEntity(Entity childEntity, int parentEntityId)
        {
            AttachEntity(childEntity, GetEntity(parentEntityId), string.Empty, null);
        }

        public void AttachEntity(Entity childEntity, Entity parentEntity)
        {
            AttachEntity(childEntity, parentEntity, string.Empty, null);
        }

        public void AttachEntity(int childEntityId, int parentEntityId, string parentTransformPath)
        {
            AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransformPath, null);
        }

        public void AttachEntity(int childEntityId, Entity parentEntity, string parentTransformPath)
        {
            AttachEntity(GetEntity(childEntityId), parentEntity, parentTransformPath, null);
        }

        public void AttachEntity(Entity childEntity, int parentEntityId, string parentTransformPath)
        {
            AttachEntity(childEntity, GetEntity(parentEntityId), parentTransformPath, null);
        }

        public void AttachEntity(Entity childEntity, Entity parentEntity, string parentTransformPath)
        {
            AttachEntity(childEntity, parentEntity, parentTransformPath, null);
        }

        public void AttachEntity(int childEntityId, int parentEntityId, Transform parentTransform)
        {
            AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransform, null);
        }

        public void AttachEntity(int childEntityId, Entity parentEntity, Transform parentTransform)
        {
            AttachEntity(GetEntity(childEntityId), parentEntity, parentTransform, null);
        }

        public void AttachEntity(Entity childEntity, int parentEntityId, Transform parentTransform)
        {
            AttachEntity(childEntity, GetEntity(parentEntityId), parentTransform, null);
        }

        public void AttachEntity(Entity childEntity, Entity parentEntity, Transform parentTransform)
        {
            AttachEntity(childEntity, parentEntity, parentTransform, null);
        }

        public void AttachEntity(int childEntityId, int parentEntityId, object userData)
        {
            AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), string.Empty, userData);
        }

        public void AttachEntity(int childEntityId, Entity parentEntity, object userData)
        {
            AttachEntity(GetEntity(childEntityId), parentEntity, string.Empty, userData);
        }

        public void AttachEntity(Entity childEntity, int parentEntityId, object userData)
        {
            AttachEntity(childEntity, GetEntity(parentEntityId), string.Empty, userData);
        }

        public void AttachEntity(Entity childEntity, Entity parentEntity, object userData)
        {
            AttachEntity(childEntity, parentEntity, string.Empty, userData);
        }

        public void AttachEntity(int childEntityId, int parentEntityId, string parentTransformPath, object userData)
        {
            AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransformPath, userData);
        }

        public void AttachEntity(int childEntityId, Entity parentEntity, string parentTransformPath, object userData)
        {
            AttachEntity(GetEntity(childEntityId), parentEntity, parentTransformPath, userData);
        }

        public void AttachEntity(Entity childEntity, int parentEntityId, string parentTransformPath, object userData)
        {
            AttachEntity(childEntity, GetEntity(parentEntityId), parentTransformPath, userData);
        }

        public void AttachEntity(Entity childEntity, Entity parentEntity, string parentTransformPath, object userData)
        {
            if (childEntity == null)
            {
                Log.Warning("Child entity is invalid.");
                return;
            }

            if (parentEntity == null)
            {
                Log.Warning("Parent entity is invalid.");
                return;
            }

            Transform parentTransform = null;
            if (string.IsNullOrEmpty(parentTransformPath))
            {
                parentTransform = parentEntity.Logic.CachedTransform;
            }
            else
            {
                parentTransform = parentEntity.Logic.CachedTransform.Find(parentTransformPath);
                if (parentTransform == null)
                {
                    Log.Warning("Can not find transform path '{0}' from parent entity '{1}'.", parentTransformPath, parentEntity.Logic.Name);
                    parentTransform = parentEntity.Logic.CachedTransform;
                }
            }

            AttachEntity(childEntity, parentEntity, parentTransform, userData);
        }

        public void AttachEntity(int childEntityId, int parentEntityId, Transform parentTransform, object userData)
        {
            AttachEntity(GetEntity(childEntityId), GetEntity(parentEntityId), parentTransform, userData);
        }

        public void AttachEntity(int childEntityId, Entity parentEntity, Transform parentTransform, object userData)
        {
            AttachEntity(GetEntity(childEntityId), parentEntity, parentTransform, userData);
        }

        public void AttachEntity(Entity childEntity, int parentEntityId, Transform parentTransform, object userData)
        {
            AttachEntity(childEntity, GetEntity(parentEntityId), parentTransform, userData);
        }

        public void AttachEntity(Entity childEntity, Entity parentEntity, Transform parentTransform, object userData)
        {
            if (childEntity == null)
            {
                Log.Warning("Child entity is invalid.");
                return;
            }

            if (parentEntity == null)
            {
                Log.Warning("Parent entity is invalid.");
                return;
            }

            if (parentTransform == null)
            {
                parentTransform = parentEntity.Logic.CachedTransform;
            }

            m_EntityManager.AttachEntity(childEntity, parentEntity, AttachEntityInfo.Create(parentTransform, userData));
        }

        public void DetachEntity(int childEntityId)
        {
            m_EntityManager.DetachEntity(childEntityId);
        }

        public void DetachEntity(int childEntityId, object userData)
        {
            m_EntityManager.DetachEntity(childEntityId, userData);
        }

        public void DetachEntity(Entity childEntity)
        {
            m_EntityManager.DetachEntity(childEntity);
        }

        public void DetachEntity(Entity childEntity, object userData)
        {
            m_EntityManager.DetachEntity(childEntity, userData);
        }

        public void DetachChildEntities(int parentEntityId)
        {
            m_EntityManager.DetachChildEntities(parentEntityId);
        }

        public void DetachChildEntities(int parentEntityId, object userData)
        {
            m_EntityManager.DetachChildEntities(parentEntityId, userData);
        }

        public void DetachChildEntities(Entity parentEntity)
        {
            m_EntityManager.DetachChildEntities(parentEntity);
        }

        public void DetachChildEntities(Entity parentEntity, object userData)
        {
            m_EntityManager.DetachChildEntities(parentEntity, userData);
        }

        public void SetEntityInstanceLocked(Entity entity, bool locked)
        {
            if (entity == null)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            IEntityGroup entityGroup = entity.EntityGroup;
            if (entityGroup == null)
            {
                Log.Warning("Entity group is invalid.");
                return;
            }

            entityGroup.SetEntityInstanceLocked(entity.gameObject, locked);
        }

        public void SetInstancePriority(Entity entity, int priority)
        {
            if (entity == null)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            IEntityGroup entityGroup = entity.EntityGroup;
            if (entityGroup == null)
            {
                Log.Warning("Entity group is invalid.");
                return;
            }

            entityGroup.SetEntityInstancePriority(entity.gameObject, priority);
        }

        private void OnShowEntitySuccess(object sender, GameFramework.Entity.ShowEntitySuccessEventArgs e)
        {
            m_EventComponent.Fire(this, ShowEntitySuccessEventArgs.Create(e));
        }

        private void OnShowEntityFailure(object sender, GameFramework.Entity.ShowEntityFailureEventArgs e)
        {
            Log.Warning("Show entity failure, entity id '{0}', asset name '{1}', entity group name '{2}', error message '{3}'.", e.EntityId, e.EntityAssetName, e.EntityGroupName, e.ErrorMessage);
            m_EventComponent.Fire(this, ShowEntityFailureEventArgs.Create(e));
        }

        private void OnShowEntityUpdate(object sender, GameFramework.Entity.ShowEntityUpdateEventArgs e)
        {
            m_EventComponent.Fire(this, ShowEntityUpdateEventArgs.Create(e));
        }

        private void OnShowEntityDependencyAsset(object sender, GameFramework.Entity.ShowEntityDependencyAssetEventArgs e)
        {
            m_EventComponent.Fire(this, ShowEntityDependencyAssetEventArgs.Create(e));
        }

        private void OnHideEntityComplete(object sender, GameFramework.Entity.HideEntityCompleteEventArgs e)
        {
            m_EventComponent.Fire(this, HideEntityCompleteEventArgs.Create(e));
        }
    }
}
