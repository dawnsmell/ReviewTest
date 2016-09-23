using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitHomming : UnitBullet
{
	public float m_ShotStartPower = 2f;
	public float m_ShotStartTime = 0;

	public float m_HommingSpeed = 1f;
	public float m_RolLevel = 0.05f;

	public bool m_ActiveTargetFind = false;
	public float m_FindRange = 10f;

	protected Transform m_Target;
	protected Transform m_Parent;

	public float m_TargetPositonHeight;
	public enum HOMMING_TYPE
	{
		MANUAL,
		AUTO,
	}

	public HOMMING_TYPE m_hommingType;
	public List<Vector3> m_ShotNodePos = new List<Vector3>();
	private int detail = 20;

	void Awake()
	{
		if( gameObject.GetComponent<AudioSource>() == null )
			gameObject.AddComponent<AudioSource>();

		m_PlayTime = 0;

		if( m_AdvanceMode )
			m_CallSubBulletList = jUtil.GetComponentsInChildren<CallUnitBulletInfo>( transform );
	}

	void Start()
	{
	}

	// Update is called once per frame
	void LateUpdate()
	{
		float deltaTime = GameManager.GameWorkerInstance.deltaTime;

		if( m_LifeTimes > 0 )
		{
			m_LifeTimes -= deltaTime;
			if( m_LifeTimes < 0 )
			{
				if( m_DieExplosion == false )
				{
					DestroyObject( gameObject );
				}
				else
				{
					GameManager.GameWorkerInstance.AttackBullet( null, null, this );
					DestroyObject( gameObject );
				}
				return;
			}
		}

		if( ( m_RepeatTimes > 0 ) && ( m_RepeatEffect != null ) )
		{
			m_RepeatTime -= deltaTime;
			if( m_RepeatTime < 0 )
			{
				m_RepeatTime = m_RepeatTimes;

				GameManager.GameWorkerInstance.CreateEffectDetail( m_RepeatEffect, transform, 0, 0, 0, Vector3.zero, 1, 1, EFFECT_DUMMY.None, 0, 0 );
			}
		}

		m_PlayTime += deltaTime;
		CallSubBulletUpdate();
	}

	public void ShotAuto( UnitController parent, Vector3 startPos, Transform target, float angle )
	{

		m_Target = target;
		if( parent != null )
		{
			m_Parent = parent.transform;
			if( m_Target == null )
			{
				float dis = 0;
				// OPT : NearEnemy find functions 통일
				//m_Target = GameManager.GameWorkerInstance.NearEnemy_AI( parent, parent.CharacterType, ref dis );
				m_Target = GameManager.GameWorkerInstance.NearEnemy( parent, ref dis );
				if( dis < m_FindRange && m_Target != null )
					m_TargetPos = m_Target.position;
				else
					m_Target = null;
			}
		}
		else
		{
			m_Parent = GameManager.GameWorkerInstance.transform;
		}

		if( m_Target == null )
			m_TargetPos = m_Parent.position + m_Parent.forward * 10;

		SetStartPos( angle );
		StartCoroutine( Move_co() );
	}

	public void SetStartPos( float angle )
	{
		m_ShotNodePos.Clear();
		Vector3 curvePos = m_Parent.position;
		Vector3 startBulletPos = transform.position;
		curvePos = startBulletPos + new Vector3( 0, m_ShotStartPower, 0 );
		transform.RotateAround( curvePos, m_Parent.forward + m_Parent.up, angle + 180f );
		m_ShotNodePos.Add( transform.position );
		transform.position = startBulletPos;
	}

	IEnumerator Move_co()
	{
		float time = 0;

		Vector3 oldPos = m_TargetPos;
		Quaternion oldRol = transform.rotation;

		if( m_hommingType == HOMMING_TYPE.AUTO )
		{
			List<Vector3> nodes = new List<Vector3>();
			nodes.Add( transform.position );

			for( int i = 0; i < m_ShotNodePos.Count; ++i )
			{
				nodes.Add( m_ShotNodePos[ i ] );
			}

			nodes.Add( m_TargetPos );
			IEnumerable<Vector3> res = Interpolate.NewCatmullRom( nodes.ToArray(), detail, false );
			List<Vector3> crPath = new List<Vector3>();
			int count = 0;

			IEnumerator iter = res.GetEnumerator();
			while( true == iter.MoveNext() )
			{
				crPath.Add( (Vector3)iter.Current );
			}

			count = crPath.Count;
			float startMovetime = m_ShotStartTime / ( detail * m_ShotNodePos.Count + 1 );
			float checkTime = 0;
			checkTime += Time.deltaTime;
			for( int i = 0; i < (int)( detail * 1.1f ); )
			{
				float value = checkTime / startMovetime;
				int nowIndex = (int)( value );
				value -= nowIndex;
				Vector3 nowP = crPath[ nowIndex ];
				Vector3 nextP = crPath[ nowIndex + 1 ];

				transform.position = Vector3.Lerp( nowP, nextP, value );
				transform.LookAt( nextP );
				i = nowIndex;
				yield return Yielders.EndOfFrame;
			}
		}

		if( gameObject.GetComponent<Rigidbody>() == null )
		{
			Rigidbody rd = gameObject.AddComponent<Rigidbody>();
			rd.useGravity = false;
		}

		time = 0;
		Vector3 lockTarget = m_TargetPos;

		while( true )
		{
			float dTime = Time.deltaTime;
			transform.position += ( transform.forward * m_HommingSpeed * dTime );
			time += dTime;

			if( time > m_LifeTimes )
				break;

			if( m_Target == null && m_ActiveTargetFind )
			{
				float dis = 0;

				// OPT : NearEnemy find function 통일
				m_Target = GameManager.GameWorkerInstance.NearEnemy( BulletFrom, ref dis );
				if( m_Target != null )
				{
					float targetDis = Vector3.SqrMagnitude( m_Target.position - transform.position );
					if( targetDis > m_FindRange * m_FindRange )
						m_Target = null;
				}
			}

			if( m_Target != null )
				lockTarget = m_Target.position + new Vector3( 0, m_TargetPositonHeight, 0 );

			oldRol = transform.rotation;
			transform.LookAt( lockTarget );
			transform.rotation = Quaternion.Lerp( oldRol, transform.rotation, m_RolLevel );
			yield return Yielders.EndOfFrame;

		}

		Destroy( gameObject );
	}
}
