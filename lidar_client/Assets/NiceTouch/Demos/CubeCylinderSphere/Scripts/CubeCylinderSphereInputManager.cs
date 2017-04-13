using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.NiceTouch
{	
	/// <summary>
	/// This persistent singleton handles the inputs and sends commands to the player
	/// </summary>
	public class CubeCylinderSphereInputManager : MonoBehaviour
	{
		
		public MMTouchControls MobileControls;

		public CharacterMovement CubeCharacter;

	    protected float _horizontalMove = 0;
	    protected float _verticalMove = 0;



	    /// <summary>
	    /// At update, we check the various commands and send them to the player.
	    /// </summary>
	    protected virtual void Update()
		{		

			if (!MobileControls.IsMobile && CubeCharacter!=null)
			{
				NonMobileCubeMovement();

				if (Input.GetButtonUp("Jump"))
				{
					NonMobileCubeJump();
				}
			}		
				
		}

		public virtual void NonMobileCubeMovement()
		{
			if (!MobileControls.IsMobile)
			{
				CubeCharacter.Move(new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")));	
			}
		}

		public virtual void NonMobileCubeJump()
		{
			CubeCharacter.Jump();
		}


	}
}