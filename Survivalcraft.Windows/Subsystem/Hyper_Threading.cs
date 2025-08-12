using Engine;
using System;
using System.Diagnostics;
//仅限电脑端
class Hyper_Threading
{
	//设置线程优先级最高(注意不是进程优先级)
	public static void SetPriority_Thread()
	{
		// 获取当前线程
		Thread currentThread = Thread.CurrentThread;
		// 修改当前线程的优先级为最高
		currentThread.Priority = ThreadPriority.Highest;
		Log.Debug("当前线程的优先级: " + currentThread.Priority);
	}
	//设置进程优先级,参数为真实时优先级,否则正常优先级
	public static void SetPriority_Process(bool realtime)
	{
		// 获取当前进程
		Process currentProcess = Process.GetCurrentProcess();
		try
		{
			// 设置当前进程的优先级
			if(realtime)
			{
				currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
				Log.Debug("进程已设置为实时优先级。");
			}
			else
			{
				currentProcess.PriorityClass = ProcessPriorityClass.Normal; 
				Log.Debug("进程已设置为正常优先级。");
			}
			
		}
		catch(Exception ex)
		{
			Log.Error("无法设置进程优先级：" + ex.Message);
		}
	}
} 