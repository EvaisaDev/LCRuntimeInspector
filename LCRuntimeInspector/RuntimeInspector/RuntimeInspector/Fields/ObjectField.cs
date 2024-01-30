using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using LCRuntimeInspector;
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
				await CreateDrawersForVariables(Array.Empty<string>(), cancellationToken: cancellationToken);
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

		internal async UniTask CreateDrawersForVariablesInternal(string[] variables, CancellationToken cancellationToken = default)
		{
            if (variables == null || variables.Length == 0)
            {
                foreach (MemberInfo variable in Inspector.GetExposedVariablesForType(Value.GetType()))
                    await CreateDrawerForVariable(variable, cancellationToken: cancellationToken);
            }
            else
            {
                foreach (MemberInfo variable in Inspector.GetExposedVariablesForType(Value.GetType()))
                {
                    if (Array.IndexOf(variables, variable.Name) >= 0)
                        await CreateDrawerForVariable(variable, cancellationToken: cancellationToken);
                }
            }
        }


        public async UniTask CreateDrawersForVariables(string[] variables, CancellationToken cancellationToken = default)
		{
            if (Value is Material item)
            {
                ShaderInspector.targetMats.Push(item);
                try
                {
                    await CreateDrawersForVariablesInternal(variables, cancellationToken: cancellationToken);
                    return;
                }
                finally
                {
                    ShaderInspector.targetMats.Pop();
                }
            }
			await CreateDrawersForVariablesInternal(variables, cancellationToken: cancellationToken);
        }

		internal async UniTask CreateDrawersForVariablesExcludingInternal(string[] variablesToExclude, CancellationToken cancellationToken = default)
		{
            if (variablesToExclude == null || variablesToExclude.Length == 0)
            {
                foreach (MemberInfo variable in Inspector.GetExposedVariablesForType(Value.GetType()))
                    await CreateDrawerForVariable(variable, cancellationToken: cancellationToken);
            }
            else
            {
                foreach (MemberInfo variable in Inspector.GetExposedVariablesForType(Value.GetType()))
                {
                    if (Array.IndexOf(variablesToExclude, variable.Name) < 0)
                        await CreateDrawerForVariable(variable, cancellationToken: cancellationToken);
                }
            }
        }


        public async UniTask CreateDrawersForVariablesExcluding(string[] variablesToExclude, CancellationToken cancellationToken = default)
		{
            if (Value is Material item)
            {
                ShaderInspector.targetMats.Push(item);
                try
                {
                    await CreateDrawersForVariablesExcludingInternal(variablesToExclude, cancellationToken);
                    return;
                }
                finally
                {
                    ShaderInspector.targetMats.Pop();
                }
            }
            await CreateDrawersForVariablesExcludingInternal(variablesToExclude, cancellationToken);
        }

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
