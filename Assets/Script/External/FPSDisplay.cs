﻿using UnityEngine;using System.Collections;public class FPSDisplay : MonoBehaviour{    protected void Start()    {        useGUILayout = false;    }    protected void Update()    {        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;    }        protected void OnGUI()    {        if( !m_UseDebug )        {            return;        }                Rect rect = new Rect( m_ScreenPos.x, m_ScreenPos.y, Screen.width, 30.0f );        float msec = m_deltaTime * 1000.0f;        float fps = 1.0f / m_deltaTime;        string text = string.Format( "{0:0.0} ms ({1:0.} fps)", msec, fps );        GUI.Label( rect, text );    }    [SerializeField] protected bool m_UseDebug      = false;    [SerializeField] protected Vector2 m_ScreenPos  = Vector2.zero;    private float m_deltaTime = 0.0f;}