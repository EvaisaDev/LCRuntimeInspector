﻿using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RuntimeInspectorNamespace
{
	public class ObjectReferenceField : InspectorField, IDropHandler
	{
#pragma warning disable 0649
		[SerializeField]
		private RectTransform referencePickerArea;

		[SerializeField]
		private PointerEventListener input;

		[SerializeField]
		private PointerEventListener inspectReferenceButton;
		private Image inspectReferenceImage;

		[SerializeField]
		protected Image background;

		[SerializeField]
		protected Text referenceNameText;
#pragma warning restore 0649

		public override void Initialize()
		{
			base.Initialize();

			input.PointerClick += ShowReferencePicker;

			if( inspectReferenceButton != null )
			{
				inspectReferenceButton.PointerClick += InspectReference;
				inspectReferenceImage = inspectReferenceButton.GetComponent<Image>();
			}
		}

		public override bool SupportsType( Type type )
		{
			return typeof( Object ).IsAssignableFrom( type );
		}

		private void ShowReferencePicker( PointerEventData eventData )
		{
			Object[] allReferences = Resources.FindObjectsOfTypeAll( BoundVariableType );

			ObjectReferencePicker.Instance.Skin = Inspector.Skin;
			ObjectReferencePicker.Instance.Show(
				( reference ) => OnReferenceChanged( (Object) reference ), null,
				( reference ) => (Object) reference ? ( (Object) reference ).name : "None",
				( reference ) => reference.GetNameWithType(),
				allReferences, (Object) Value, true, "Select " + BoundVariableType.Name, Inspector.Canvas );
		}

		private void InspectReference( PointerEventData eventData )
		{
			if( Value != null && !Value.Equals( null ) )
			{
				if( Value is Component )
					Inspector.InspectInternal( ( (Component) Value ).gameObject ).Forget();
				else
					Inspector.InspectInternal( Value ).Forget();
			}
		}

		protected override async UniTask OnBound( MemberInfo variable, CancellationToken cancellationToken = default )
		{
			await base.OnBound( variable, cancellationToken );
			OnReferenceChanged( (Object) Value );
		}

		protected virtual void OnReferenceChanged( Object reference )
		{
			if( (Object) Value != reference )
				Value = reference;

			if( referenceNameText != null )
				referenceNameText.text = reference.GetNameWithType( BoundVariableType );

			if( inspectReferenceButton != null )
				inspectReferenceButton.gameObject.SetActive( Value != null && !Value.Equals( null ) );

			Inspector.RefreshDelayed();
		}

		public void OnDrop( PointerEventData eventData )
		{
			Object assignableObject = (Object) RuntimeInspectorUtils.GetAssignableObjectFromDraggedReferenceItem( eventData, BoundVariableType );
			if( assignableObject )
				OnReferenceChanged( assignableObject );
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();

			background.color = Skin.InputFieldNormalBackgroundColor.Tint( 0.075f );

			referenceNameText.SetSkinInputFieldText( Skin );

			referenceNameText.resizeTextMinSize = Mathf.Max( 2, Skin.FontSize - 2 );
			referenceNameText.resizeTextMaxSize = Skin.FontSize;

			if( inspectReferenceImage )
			{
				inspectReferenceImage.color = Skin.TextColor.Tint( 0.1f );
				inspectReferenceImage.GetComponent<LayoutElement>().SetWidth( Mathf.Max( Skin.LineHeight - 8, 6 ) );
			}

			if( referencePickerArea )
			{
				Vector2 rightSideAnchorMin = new Vector2( Skin.LabelWidthPercentage, 0f );
				variableNameMask.rectTransform.anchorMin = rightSideAnchorMin;
				referencePickerArea.anchorMin = rightSideAnchorMin;
			}
		}

		public override async UniTask Refresh(CancellationToken cancellationToken)
		{
			object lastValue = Value;
			await base.Refresh(cancellationToken);

			if( lastValue != Value )
				OnReferenceChanged( (Object) Value );
		}
	}
}
