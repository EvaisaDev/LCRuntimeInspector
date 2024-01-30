﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class GameObjectField : ExpandableInspectorField
	{
		protected override int Length { get { return components.Count + 4; } } // 4: active, name, tag, layer

		private string currentTag = null;

		private Getter isActiveGetter, nameGetter, tagGetter;
		private Setter isActiveSetter, nameSetter, tagSetter;
		private PropertyInfo layerProp;

		private readonly List<Component> components = new List<Component>( 8 );
		private readonly List<bool> componentsExpandedStates = new List<bool>();

		private Type[] addComponentTypes;

		internal static ExposedMethod addComponentMethod = new ExposedMethod( typeof( GameObjectField ).GetMethod( "AddComponentButtonClicked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ), new RuntimeInspectorButtonAttribute( "Add Component", false, ButtonVisibility.InitializedObjects ), false );
		internal static ExposedMethod removeComponentMethod = new ExposedMethod( typeof( GameObjectField ).GetMethod( "RemoveComponentButtonClicked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static ), new RuntimeInspectorButtonAttribute( "Remove Component", false, ButtonVisibility.InitializedObjects ), true );

		public override void Initialize()
		{
			base.Initialize();

			isActiveGetter = () => ( (GameObject) Value ).activeSelf;
			isActiveSetter = ( value ) => ( (GameObject) Value ).SetActive( (bool) value );

			nameGetter = () => ( (GameObject) Value ).name;
			nameSetter = ( value ) =>
			{
				( (GameObject) Value ).name = (string) value;
				NameRaw = Value.GetNameWithType();

				RuntimeHierarchy hierarchy = Inspector.ConnectedHierarchy;
				if( hierarchy )
					hierarchy.RefreshNameOf( ( (GameObject) Value ).transform );
			};

			tagGetter = () =>
			{
				GameObject go = (GameObject) Value;
				if( !go.CompareTag( currentTag ) )
					currentTag = go.tag;

				return currentTag;
			};
			tagSetter = ( value ) => ( (GameObject) Value ).tag = (string) value;

			layerProp = typeof( GameObject ).GetProperty( "layer" );
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof( GameObject );
		}

		protected override async UniTask OnBound(MemberInfo variable, CancellationToken cancellationToken = default)
		{
			await base.OnBound( variable, cancellationToken );
			currentTag = ( (GameObject) Value ).tag;
		}

		protected override async UniTask OnUnbound(CancellationToken cancellationToken = default)
		{
			await base.OnUnbound(cancellationToken);

			components.Clear();
			componentsExpandedStates.Clear();
		}

		protected override async UniTask ClearElements(CancellationToken cancellationToken = default)
		{
			componentsExpandedStates.Clear();
			for( int i = 0; i < elements.Count; i++ )
			{
				// Don't keep track of non-expandable drawers' or destroyed components' expanded states
				if( elements[i] is ExpandableInspectorField && ( elements[i].Value as Object ) )
					componentsExpandedStates.Add( ( (ExpandableInspectorField) elements[i] ).IsExpanded );
			}

			await base.ClearElements(cancellationToken);
		}

		protected override async UniTask GenerateElements(CancellationToken cancellationToken = default)
		{
			if( components.Count == 0 )
				return;

			await CreateDrawer( typeof( bool ), "Is Active", isActiveGetter, isActiveSetter, cancellationToken: cancellationToken );
			StringField nameField = await CreateDrawer( typeof( string ), "Name", nameGetter, nameSetter, cancellationToken: cancellationToken ) as StringField;
			StringField tagField = await CreateDrawer( typeof( string ), "Tag", tagGetter, tagSetter, cancellationToken: cancellationToken ) as StringField;
			await CreateDrawerForVariable( layerProp, "Layer", cancellationToken );

			for( int i = 0, j = 0; i < components.Count; i++ )
			{
				InspectorField componentDrawer = await CreateDrawerForComponent( components[i], cancellationToken: cancellationToken );
				if( componentDrawer as ExpandableInspectorField && j < componentsExpandedStates.Count && componentsExpandedStates[j++] )
					( (ExpandableInspectorField) componentDrawer ).IsExpanded = true;
			}

			if( nameField )
				nameField.SetterMode = StringField.Mode.OnSubmit;

			if( tagField )
				tagField.SetterMode = StringField.Mode.OnSubmit;

			if( Inspector.ShowAddComponentButton )
				await CreateExposedMethodButton( addComponentMethod, () => this, ( value ) => { }, cancellationToken );

			componentsExpandedStates.Clear();
		}

		public override async UniTask Refresh(CancellationToken cancellationToken)
		{
			// Refresh components
			components.Clear();
			GameObject go = Value as GameObject;
			if( go )
			{
				go.GetComponents( components );

				for( int i = components.Count - 1; i >= 0; i-- )
				{
					if( !components[i] )
						components.RemoveAt( i );
				}

				if( Inspector.ComponentFilter != null )
					Inspector.ComponentFilter( go, components );
			}

			// Regenerate components' drawers, if necessary
			await base.Refresh(cancellationToken);
		}

		[UnityEngine.Scripting.Preserve] // This method is bound to addComponentMethod
		private void AddComponentButtonClicked()
		{
			GameObject target = (GameObject) Value;
			if( !target )
				return;

			if( addComponentTypes == null )
			{
				List<Type> componentTypes = new List<Type>( 128 );

#if UNITY_EDITOR || !NETFX_CORE
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
#else
				// Common Unity assemblies
				IEnumerable<Assembly> assemblies = new HashSet<Assembly>
				{
					typeof( Transform ).Assembly,
					typeof( RectTransform ).Assembly,
					typeof( Rigidbody ).Assembly,
					typeof( Rigidbody2D ).Assembly,
					typeof( AudioSource ).Assembly
				};
#endif
				// Search assemblies for Component types
				foreach( Assembly assembly in assemblies )
				{
#if( NET_4_6 || NET_STANDARD_2_0 ) && ( UNITY_EDITOR || !NETFX_CORE )
					if( assembly.IsDynamic )
						continue;
#endif
					try
					{
						foreach( Type type in assembly.GetExportedTypes() )
						{
							if( !typeof( Component ).IsAssignableFrom( type ) )
								continue;

#if UNITY_EDITOR || !NETFX_CORE
							if( type.IsGenericType || type.IsAbstract )
#else
							if( type.GetTypeInfo().IsGenericType || type.GetTypeInfo().IsAbstract )
#endif
								continue;

							componentTypes.Add( type );
						}
					}
					catch( NotSupportedException ) { }
					catch( System.IO.FileNotFoundException ) { }
					catch( Exception e )
					{
						Debug.LogError( "Couldn't search assembly for Component types: " + assembly.GetName().Name + "\n" + e.ToString() );
					}
				}

				addComponentTypes = componentTypes.ToArray();
			}

			ObjectReferencePicker.Instance.Skin = Inspector.Skin;
			ObjectReferencePicker.Instance.Show(
				null, ( type ) =>
				{
					// Make sure that RuntimeInspector is still inspecting this GameObject
					if( type != null && target && Inspector && ( Inspector.InspectedObject as GameObject ) == target )
					{
						target.AddComponent( (Type) type );
						Inspector.Refresh().Forget();
					}
				},
				( type ) => ( (Type) type ).FullName,
				( type ) => ( (Type) type ).FullName,
				addComponentTypes, null, false, "Add Component", Inspector.Canvas );
		}

		[UnityEngine.Scripting.Preserve] // This method is bound to removeComponentMethod
		private static void RemoveComponentButtonClicked( ExpandableInspectorField componentDrawer )
		{
			if( !componentDrawer || !componentDrawer.Inspector )
				return;

			Component component = componentDrawer.Value as Component;
			if( component && !( component is Transform ) )
				RemoveComponentAsync( component, componentDrawer.Inspector ).Forget();
		}

		private static async UniTask RemoveComponentAsync( Component component, RuntimeInspector inspector )
		{
			Destroy( component );

			// Destroy operation doesn't take place immediately, wait for the component to be fully destroyed
            await UniTask.NextFrame();

			await inspector.Refresh();
			inspector.EnsureScrollViewIsWithinBounds(); // Scroll view's contents can get out of bounds after removing a component
		}
	}
}
