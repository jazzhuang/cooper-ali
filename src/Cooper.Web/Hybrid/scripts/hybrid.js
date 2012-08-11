﻿//Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/
/////////////////////////////////////////////////////////////////////////////////////////

//JS 与 Native 交互接口格式约定
//function Result { status:true, data: {}|true, message:'' }

//refresh("Login", [{ username: 'xuehua', password: '123456', type:'anonymous|normal' }], function (result) { return new Result(); });
//refresh("Logout", [{ username: 'xuehua' }], function (result) { return new Result(); });
//refresh("SyncTaskList", [{ username: 'xuehua', tasklistid: '123456' }], function (result) { return new Result(); });
//refresh("SyncTasks", [{ username: 'xuehua', tasklistid: '123456' }], function (result) { return new Result(); });

//getData("GetNetworkStatus", [], function (result) { return { true }; });
//getData("GetCurrentUser", [], function (result) { return new Result(); });
//getData("GetTasklists", [{ username: 'xuehua' }], function (result) { return json; });
//getData("GetTasksByPriority", [{ username: 'xuehua', tasklistId = 'id' }], function (result) { { Editable:true,tasks:[],sorts:[] });

//saveData("CreateTasklist", [{ username: 'xuehua', id: 'id', name: 'list 1', type: 'personal' }], function (result) { return new Result();  });
//saveData("CreateTask", [{ username: 'xuehua', tasklistId: 'id', task: {}, changes: $.toJSON(changes) }], function (result) { return new Result(); });
//saveData("UpdateTask", [{ username: 'xuehua', tasklistId: 'id', task: {}, changes: $.toJSON(changes) }], function (result) { return new Result(); });
//saveData("DeleteTask", [{ username: 'xuehua', tasklistId: 'id', taskId: ''], function (result) { return new Result(); });

/////////////////////////////////////////////////////////////////////////////////////////

//Web接口地址声明
var web_loginUrl = "../Account/Login";
var web_logoutUrl = "../Account/Logout";
var web_getTasklistsUrl = "../Personal/GetTasklists";
var web_createTaskListUrl = "../Personal/CreateTasklist";
var web_getTasksUrl = "../Personal/GetByPriority";
var web_syncTaskUrl = "../Personal/Sync";

//Native接口地址声明
var native_loginUrl = "CooperPlugin/refresh";
var native_logoutUrl = "CooperPlugin/refresh";
var native_syncTasklistsUrl = "CooperPlugin/refresh";
var native_getNetworkStatusUrl = "CooperPlugin/get";
var native_getCurrentUserUrl = "CooperPlugin/get";
var native_getTasklistsUrl = "CooperPlugin/get";
var native_getTasksByPriorityUrl = "CooperPlugin/get";
var native_createTasklistUrl = "CooperPlugin/save";
var native_createTaskUrl = "CooperPlugin/save";
var native_updateTaskUrl = "CooperPlugin/save";
var native_deleteTaskUrl = "CooperPlugin/save";

//新增的本地任务列表的id的前缀
var newTaskListTempIdPrefix = "temp_";
//新增的本地任务的id的前缀
var newTaskTempIdPrefix = "temp_";

//任务对象定义
var Task = function () {
    this.id = generateNewTaskId();
    this.subject = "";
    this.body = "";
    this.priority = "0";
    this.dueTime = "";
    this.isCompleted = "false";
    this.tags = [];
    this.isEditable = true;
    this.isNew = false;
    this.isDirty = false;
    this.isDeleted = false;
};
//任务列表对象定义
var TaskList = function () {
    this.id = generateNewTaskListId();
    this.name = "";
    this.isEditable = true;
};
//Task修改日志信息对象
var ChangeLog = function () {
    this.Type = 0;
    this.ID = "";
    this.Name = "";
    this.Value = "";
};
//Task排序信息
var Sort = function () {
    this.By = 0;
    this.Key = "";
    this.Name = "";
    this.Indexs = [];
};

//创建一个唯一标识
function generateUUID() {
    var d = new Date().getTime();
    var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = (d + Math.random() * 16) % 16 | 0;
        d = Math.floor(d / 16);
        return (c == 'x' ? r : (r & 0x7 | 0x8)).toString(16);
    });
    return uuid;
}
//生成一个唯一的任务列表标识
function generateNewTaskListId() {
    return newTaskListTempIdPrefix + generateUUID();
}
//生成一个唯一的任务标识
function generateNewTaskId() {
    return newTaskTempIdPrefix + generateUUID();
}
//返回给定的单个Task的ChangeLog信息
function getTaskChanges(task) {
    var changeLogs = [];
    var changeLog = new ChangeLog();

    if (task.isDeleted) {
        changeLog.Type = 1;
        changeLog.ID = task.id;
        changeLogs.push(changeLog);
        return changeLogs;
    }
    else if (task.isNew || task.isDirty) {
        changeLog.ID = task.id;
        changeLog.Name = "subject";
        changeLog.Value = task.subject;
        changeLogs.push(changeLog);

        changeLog = new ChangeLog();
        changeLog.ID = task.id;
        changeLog.Name = "body";
        changeLog.Value = task.body;
        changeLogs.push(changeLog);

        changeLog = new ChangeLog();
        changeLog.ID = task.id;
        changeLog.Name = "priority";
        changeLog.Value = task.priority;
        changeLogs.push(changeLog);

        changeLog = new ChangeLog();
        changeLog.ID = task.id;
        changeLog.Name = "dueTime";
        changeLog.Value = task.dueTime;
        changeLogs.push(changeLog);

        changeLog = new ChangeLog();
        changeLog.ID = task.id;
        changeLog.Name = "isCompleted";
        changeLog.Value = task.isCompleted;
        changeLogs.push(changeLog);
    }

    return changeLogs;
}
//判断当前访问的是否在移动设备
function isMobileDevice() {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry/i.test(navigator.userAgent);
}
//与Web API进行交互
function callWebAPI(url, data, callback) {
    $.ajax({
        url: url,
        data: data,
        type: 'POST',
        dataType: 'json',
        cache: false,
        async: true,
        beforeSend: function () { },
        success: function (data) {
            if (callback) {
                callback(data);
            }
        }
    });
}
//与PhoneGap Native API进行交互
function callNativeAPI(url, data, callback) {
    var items = url.split("/");
    var serviceName = items[0];
    var actionName = items[1].toLowerCase();

    //因为参数必须是数组，所以把参数放在一个数组中
    var params = [];
    params.push(data);

    //调用Native接口
    Cordova.exec(
            function (result) {
                if (callback != null) {
                    callback(result);
                }
            },
            function () { },
            serviceName,
            actionName,
            params
        );
}

//在当前网络可用的情况下调用指定函数
function callIfNetworkAvailable(fn) {
    if (fn == null) {
        return;
    }
    getNetworkStatus(function (result) {
        if (result.status) {
            if (result.data) {
                if (fn != null) {
                    fn();
                }
            }
            else {
                alert(lang.networkUnAvailable);
            }
        }
        else {
            alert(lang.getNetworkAvailableStatusFailed);
        }
    });
}
//获取当前登录用户后调用指定函数
function callAfterGetCurrentUser(fn) {
    if (fn == null) {
        return;
    }
    callIfNetworkAvailable(function() {
        getCurrentUser(function (result) {
            if (result.status) {
                fn(result.data.username);
            }
            else {
                alert(lang.getCurrentUserFailed);
            }
        });
    });
}
//用户登录
function login(userName, password, type, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callIfNetworkAvailable(function() {
            callNativeAPI(
                native_loginUrl,
                { key: 'Login', username: userName, password: password, type: type },
                function (result) {
                    callback(result);
                }
            );
        });
    }
    else {
        callWebAPI(
            web_loginUrl,
            { username: userName, password: password },
            function (result) {
                callback({ status: result, data: {}, message:'' });
            }
        );
    }
}
//用户注销
function logout(callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_logoutUrl,
                { key: 'Logout', username: currentUser },
                function (result) {
                    callback(result);
                }
            );
        });
    }
    else {
        callWebAPI(
            web_logoutUrl,
            { },
            function (result) {
                callback({ status: true, data: {}, message:'' });
            }
        );
    }
}
//获取当前网络状态
function getNetworkStatus(callback) {
    if (callback == null) {
        return;
    }
    callNativeAPI(
        native_getNetworkStatusUrl,
        { key: 'GetNetworkStatus' },
        function (result) {
            callback(result);
        }
    );
}
//获取当前用户
function getCurrentUser(callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callNativeAPI(
            native_getCurrentUserUrl,
            { key: 'GetCurrentUser' },
            function (result) {
                callback(result);
            }
        );
    }
}
//获取当前用户的所有任务表
function getTasklists(callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_getTasklistsUrl,
                { key: 'GetTasklists', username: currentUser },
                function (result) {
                    //TODO:xiaoxuan
                    if(result.status) {
                        var taskLists = [];
                        for (key in result.data) {
                            var taskList = new TaskList();
                            taskList.id = key;
                            taskList.name = result.data[key];
                            taskList.isEditable = true;
                            taskLists.push(taskList);
                        }
                        callback({ status: true, data: { taskLists: taskLists }, message: '' });
                    }
                }
            );
        });
    }
    else {
        callWebAPI(
            web_getTasklistsUrl,
            { },
            function (result) {
                var taskLists = [];
                for (key in result) {
                    var taskList = new TaskList();
                    taskList.id = key;
                    taskList.name = result[key];
                    taskList.isEditable = true;
                    taskLists.push(taskList);
                }
                callback({ status: true, data: { taskLists: taskLists }, message: '' });
            }
        );
    }
}
//获取当前用户的所有任务表
function getTasksByPriority(tasklistId, isCompleted, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_getTasksByPriorityUrl,
                { key: 'GetTasksByPriority', username: currentUser, tasklistId: tasklistId },
                function (result) {
                    var tasks = [];
                    var tasksFromNative = result.data.tasks;
                    for (var index = 0; index < tasksFromNative.length; index++) {
                        var taskFromNative = tasksFromNative[index];

                        //过滤出不符合是否完成条件的任务
                        if (isCompleted == "true" || isCompleted == "false") {
                            if (taskFromNative["isCompleted"] == null || taskFromNative["isCompleted"].toString() != isCompleted) {
                                continue;
                            }
                        }

                        var task = new Task();
                        task.id = taskFromNative["id"];
                        task.subject = taskFromNative["subject"] == null ? "" : taskFromNative["subject"];
                        task.body = taskFromNative["body"] == null ? "" : taskFromNative["body"];
                        task.dueTime = taskFromNative["dueTime"] == null ? "" : taskFromNative["dueTime"];
                        task.priority = taskFromNative["priority"] == null ? "" : taskFromNative["priority"];
                        task.isCompleted = taskFromNative["isCompleted"] == null ? "" : taskFromNative["isCompleted"];
                        task.isEditable = result.data.editable;
                        tasks.push(task);
                    }
                    if (callback != null) {
                        callback({ status: true, data: { tasks: tasks }, message: '' });
                    }
                }
            );
        });
    }
    else {
        callWebAPI(
            web_getTasksUrl,
            { tasklistId: tasklistId },
            function (result) {
                var tasks = [];
                var tasksFromServer = result != null && result.List != null ? result.List : [];
                for (var index = 0; index < tasksFromServer.length; index++) {
                    var taskFromServer = tasksFromServer[index];

                    //过滤出不符合是否完成条件的任务
                    if (isCompleted == "true" || isCompleted == "false") {
                        if (taskFromServer["IsCompleted"] == null || taskFromServer["IsCompleted"].toString() != isCompleted) {
                            continue;
                        }
                    }

                    var task = new Task();
                    task.id = taskFromServer["ID"];
                    task.subject = taskFromServer["Subject"] == null ? "" : taskFromServer["Subject"];
                    task.body = taskFromServer["Body"] == null ? "" : taskFromServer["Body"];
                    task.dueTime = taskFromServer["DueTime"] == null ? "" : taskFromServer["DueTime"];
                    task.priority = taskFromServer["Priority"] == null ? "" : taskFromServer["Priority"];
                    task.isCompleted = taskFromServer["IsCompleted"] == null ? "" : taskFromServer["IsCompleted"];
                    task.isEditable = taskFromServer["Editable"] == null ? "" : taskFromServer["Editable"];
                    tasks.push(task);
                }
                if (callback != null) {
                    callback({ status: true, data: { tasks: tasks }, message: '' });
                }
            }
        );
    }
}
//创建一个任务列表
function createTasklist(id, name, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_createTasklistUrl,
                { key: 'CreateTasklist', username: currentUser, id: id, name: name, type: 'personal' },
                function (result) {
                    callback(result);
                }
            );
        });
    }
    else {
        callWebAPI(
            web_createTaskListUrl,
            { name: taskListName, type: "personal" },
            function (result) {
                callback({ status: true, data: { }, message: '' });
            }
        );
    }
}
//创建一个任务
function createTask(tasklistId, task, changes, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_createTaskUrl,
                //TODO:xiaoxuan
                { key: 'CreateTask', username: currentUser, tasklistId: tasklistId, task: task, changes: changes },
                function (result) {
                    callback(result);
                }
            );
        });
    }
    else {
        callWebAPI(
            web_syncTaskUrl,
            { tasklistId: tasklistId, tasklistChanges: null, changes: $.toJSON(changes), by: "ByPriority", sorts: null },
            function (result) {
                callback({ status: true, data: { }, message: '' });
            }
        );
    }
}
//更新一个任务
function updateTask(tasklistId, task, changes, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_updateTaskUrl,
                { key: 'UpdateTask', username: currentUser, tasklistId: tasklistId, task: $.toJSON(task), changes: $.toJSON(changes) },
                function (result) {
                    callback(result);
                }
            );
        });
    }
    else {
        callWebAPI(
            web_syncTaskUrl,
            { tasklistId: tasklistId, tasklistChanges: null, changes: $.toJSON(changes), by: "ByPriority", sorts: null },
            function (result) {
                callback({ status: true, data: { }, message: '' });
            }
        );
    }
}
//删除一个任务
function deleteTask(tasklistId, taskId, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_deleteTaskUrl,
                { key: 'DeleteTask', username: currentUser, tasklistId: tasklistId, taskId: taskId },
                function (result) {
                    callback(result);
                }
            );
        });
    }
    else {
        callWebAPI(
            web_syncTaskUrl,
            { tasklistId: tasklistId, tasklistChanges: null, changes: $.toJSON(changes), by: "ByPriority", sorts: null },
            function (result) {
                callback({ status: true, data: { }, message: '' });
            }
        );
    }
}
//同步指定的任务列表
function syncTaskLists(tasklistId, callback) {
    if (callback == null) {
        return;
    }
    if (isMobileDevice()) {
        callAfterGetCurrentUser(function (currentUser) {
            callNativeAPI(
                native_syncTasklistsUrl,
                { key: 'SyncTasklists', username: currentUser, tasklistid: tasklistId },
                function (result) {
                    callback(result);
                }
            );
        });
    }
}