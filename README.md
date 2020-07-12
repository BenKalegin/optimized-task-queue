# optmized-task-queue


C# std/core component to replace `await Task.WhenAny(..)` operation when number of task being waiting upon is significant (hundreds - thousands).

It could help to save number of compute instance or containers for applications like web crowlers, webhook fan-outs, decoupled microservices, or distributed workload.

Problem with `await Task.WhenAny(..)` is that its performance (coupled with succeding remove of task from array) is N(O^2). 

