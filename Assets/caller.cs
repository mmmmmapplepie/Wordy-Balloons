using System.Collections.Generic;
using UnityEngine;
using System;
public class caller : MonoBehaviour
{
	public holder h;
	Action incre;
	void Start()
	{
		incre = h.ReturnIncrementMethod();
	}
	public void btnPress()
	{
		incre();
	}
}
