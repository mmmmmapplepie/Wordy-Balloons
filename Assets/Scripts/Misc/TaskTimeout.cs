using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TaskTimeout : MonoBehaviour {
	public const float DefaultTimeoutTime = 15;
	static TimeSpan myTimeout = TimeSpan.FromSeconds(DefaultTimeoutTime);
	public static async Task<T> AddTimeout<T>(Task<T> task, TimeSpan? time = null) {
		Task delayTask = Task.Delay(time == null ? myTimeout : (TimeSpan)time);
		Task completedTask = await Task.WhenAny(task, delayTask);

		if (completedTask == delayTask)
			throw new TimeoutException("The operation timed out.");

		return await task;
	}
	public static async Task AddTimeout(Task task, TimeSpan? time = null) {
		Task delayTask = Task.Delay(time == null ? myTimeout : (TimeSpan)time);
		Task completedTask = await Task.WhenAny(task, delayTask);

		if (completedTask == delayTask)
			throw new TimeoutException("The operation timed out.");

		await task;
	}
}
