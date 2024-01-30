using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	public class ObjectField : ExpandableInspectorField
	{
#pragma warning disable 0649
		[SerializeField]
		private Button initializeObjectButton;
#pragma warning restore 0649

		private bool elementsInitialized = false;
		private IRuntimeInspectorCustomEditor customEditor;

		protected override int Length
		{
			get
			{
				if( Value.IsNull() )
				{
					if( !initializeObjectButton.gameObject.activeSelf )
						return -1;

					return 0;
				}

				if( initializeObjectButton.gameObject.activeSelf )
					return -1;

				if( !elementsInitialized )
				{
					elementsInitialized = true;
					return -1;
				}

				return elements.Count;
			}
		}

		public override void Initialize()
		{
			base.Initialize();
            initializeObjectButton.onClick.AddListener(UniTask.UnityAction(async () => await InitializeObject()));
        }

		public override bool SupportsType( Type type )
		{
			return true;
		}

		protected override async UniTask OnBound( MemberInfo variable, CancellationToken cancellationToken = default )
		{
			elementsInitialized = false;
			await base.OnBound( variable, cancellationToken );
		}

		protected override async UniTask GenerateElements(CancellationToken cancellationToken = default)
		{
			if( Value.IsNull() )
			{
				initializeObjectButton.gameObject.SetActive( CanInitializeNewObject() );
				return;
			}

			initializeObjectButton.gameObject.SetActive( false );

			if( ( customEditor = RuntimeInspectorUtils.GetCustomEditor( Value.GetType() ) ) != null )
				await customEditor.GenerateElements( this, cancellationToken );
			else
				await CreateDrawersForVariables(cancellationToken);
		}

		protected override async UniTask ClearElements(CancellationToken cancellationToken = default)
		{
			await base.ClearElements(cancellationToken);

			if( customEditor != null )
			{
				await customEditor.Cleanup(cancellationToken);
				customEditor = null;
			}
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			initializeObjectButton.SetSkinButton( Skin );
		}

		public override async UniTask Refresh(CancellationToken cancellationToken)
		{
			await base.Refresh(cancellationToken);

			if( customEditor != null )
				await customEditor.Refresh(cancellationToken);
		}

        protected async UniTask CreateDrawersForMembers(IEnumerable<MemberInfo> members, CancellationToken cancellationToken = default)
        {
            await UniTask.WhenAll(
                members.Select(member => CreateDrawerForVariable(member, cancellationToken: cancellationToken))
            );
        }

        public async UniTask CreateDrawersForVariables(CancellationToken cancellationToken)
        {
            await CreateDrawersForMembers(ExposedVariablesForValueType, cancellationToken);
        }

        public async UniTask CreateDrawersForVariables(MemberFilter filter, object filterCriteria, CancellationToken cancellationToken = default)
        {
            var members = ExposedVariablesForValueType.Where(FilterAcceptsMember);
            await CreateDrawersForMembers(members, cancellationToken);

            bool FilterAcceptsMember(MemberInfo member) => filter(member, filterCriteria);
        }

        protected virtual IEnumerable<MemberInfo> ExposedVariablesForValueType => Inspector.GetExposedVariablesForType(Value.GetType());

        private bool CanInitializeNewObject()
		{
#if UNITY_EDITOR || !NETFX_CORE
			if( BoundVariableType.IsAbstract || BoundVariableType.IsInterface )
#else
			if( BoundVariableType.GetTypeInfo().IsAbstract || BoundVariableType.GetTypeInfo().IsInterface )
#endif
				return false;

			if( typeof( ScriptableObject ).IsAssignableFrom( BoundVariableType ) )
				return true;

			if( typeof( UnityEngine.Object ).IsAssignableFrom( BoundVariableType ) )
				return false;

			if( BoundVariableType.IsArray )
				return false;

#if UNITY_EDITOR || !NETFX_CORE
			if( BoundVariableType.IsGenericType && BoundVariableType.GetGenericTypeDefinition() == typeof( List<> ) )
#else
			if( BoundVariableType.GetTypeInfo().IsGenericType && BoundVariableType.GetGenericTypeDefinition() == typeof( List<> ) )
#endif
				return false;

			return true;
		}

		private async UniTask InitializeObject()
		{
			if( CanInitializeNewObject() )
			{
				Value = BoundVariableType.Instantiate();

				await RegenerateElements();
				IsExpanded = true;
			}
		}
	}
}
