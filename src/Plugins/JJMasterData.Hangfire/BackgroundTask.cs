﻿using System;
using System.Collections.Generic;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using JJMasterData.Commons.Language;
using JJMasterData.Commons.Tasks;
using JJMasterData.Commons.Tasks.Progress;

namespace JJMasterData.Hangfire;

public sealed class BackgroundTask : IBackgroundTask
{
    private static BackgroundTask _instance;

    public static BackgroundTask GetInstance()
    {
        return _instance ??= new BackgroundTask();
    }

    private static Lazy<List<TaskWrapper>> _taskList;
    internal static List<TaskWrapper> TaskList
    {
        get
        {
            _taskList ??= new Lazy<List<TaskWrapper>>();

            return _taskList.Value;
        }
    }

    internal TaskWrapper GetTask(string key)
    {
        return TaskList.Find(x => x.Key.Equals(key));
    }

    public void Run(string key, IBackgroundTaskWorker worker)
    {
        if (IsRunning(key))
            throw new Exception(Translate.Key("Background task is already running."));

        var taskWrapper = new TaskWrapper
        {
            Key = key,
            TaskWorker = worker
        };

        var olderPipeline = GetTask(key);
        if (olderPipeline != null)
            TaskList.Remove(olderPipeline);

        //Workaround: Interfaces are not a good idea with Hangfire.
        var taskTrigger = new TaskTrigger();
        taskWrapper.JobId = taskTrigger.RunInBackground(key, worker);

        TaskList.Add(taskWrapper);
    }

    public bool IsRunning(string key)
    {
        var taskWrapper = GetTask(key);

        if (taskWrapper == null)
            return false;

        var connection = JobStorage.Current.GetConnection();
        JobData jobData = connection.GetJobData(taskWrapper.JobId);

        return jobData.State.Equals(ProcessingState.StateName);
    }

    public void Abort(string key)
    {
        var taskWrapper = GetTask(key);
        if (taskWrapper == null)
            return;

        BackgroundJob.Delete(taskWrapper.JobId);
    }

    public T GetProgress<T>(string key) where T : IProgressReporter
    {
        TaskWrapper task = GetTask(key);
        if (task != null)
            return (T)task.ProgressResult;

        return default;
    }

    internal void SetProgress(string key, IProgressReporter progress)
    {
        var task = TaskList.Find(x => x.Key.Equals(key));
        if (task != null)
            task.ProgressResult = progress;
    }

}
