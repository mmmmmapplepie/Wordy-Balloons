using System.Collections.Generic;
using UnityEngine;
using System;

public class holder : MonoBehaviour
{
	void Update()
	{
		// print(i);
	}
	public Action ReturnIncrementMethod()
	{
		return increment;
	}

	public int i = 0;
	void increment()
	{
		Debug.LogWarning("called");
		i++;
	}

}
