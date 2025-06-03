using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TaskTimeout : MonoBehaviour {
	static TimeSpan myTimeout = TimeSpan.FromSeconds(10);
	public static async Task<T> AddTimeout<T>(Task<T> task) {
		Task delayTask = Task.Delay(myTimeout);
		Task completedTask = await Task.WhenAny(task, delayTask);

		if (completedTask == delayTask)
			throw new TimeoutException("The operation timed out.");

		return await task;
	}
	public static async Task AddTimeout(Task task) {
		Task delayTask = Task.Delay(myTimeout);
		Task completedTask = await Task.WhenAny(task, delayTask);

		if (completedTask == delayTask)
			throw new TimeoutException("The operation timed out.");

		await task;
	}
}
