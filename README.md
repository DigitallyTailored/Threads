# Thread Management Library

This library provides utilities for managing threads in C#. It offers a flexible system for creating and handling multiple thread pools, executing tasks asynchronously and synchronously, and easily controlling the execution of tasks using a simple and intuitive API.

The system is designed to work on half of the system's available threads per pool. This default behavior can be easily overridden and custom settings can be applied when creating a pool.

## Usage

### Creating a Thread Pool

You can create additional thread pools using the `Threads.CreatePool(int poolNumber)` method. This allows you to queue async tasks separately. For instance, you might have one pool focused on loading terrain chunks and another for handling Level of Detail (LoD). Tasks in different pools can run in parallel.

```csharp
Threads.CreatePool(2);
```

### Queueing Tasks

#### Asynchronous Tasks

Tasks can be queued to run in the background using the `Threads.Async(Action task)` method. If no pool number is specified, the task is queued in the default thread pool (1).

```csharp
Threads.Async(() =>
{
    // stuff to run
});
```

To queue a task in a specific thread pool, use the `Threads.Async(int poolNumber, Action task)` method.

```csharp
Threads.Async(2, // specify a pool to run this task in
() =>
{
    // stuff to run
});
```

#### Synchronous Tasks

Tasks can be queued to run on the main thread using the `Threads.Sync(Action task)` method.

```csharp
Threads.Sync(() =>
{
    // stuff to run
});
```

### Run Synchronous Tasks

`Threads.RunSync(int millisecondsTimeout)` method is used to run tasks in the main loop. You can set a custom maximum runtime for tasks to be ran. 

```csharp
Threads.RunSync(13);
```

## Examples

### Stacking Tasks

#### Async > Sync

```csharp
Threads.Async(() =>
{
    // async task

    Threads.Sync(() =>
    {
        // sync task
    });
});
```

#### Sync > Async > Sync > Async

```csharp
Threads.Sync(() =>
{
    // sync task 1

    Threads.Async(() =>
    {
        // async task 1

        Threads.Sync(() =>
        {
            // sync task 2

            Threads.Async(() =>
            {
                // async task 2
            });
        });
    });
});
```

## Notes

The thread pool size is set to half of the system's available threads per pool. However, this can be easily overridden and a different size can be set when creating a pool.