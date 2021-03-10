module.exports = {
  apps: [{
    name: "ChuckDeviceController",
    script: "ChuckDeviceController.dll",
    watch: true,
    cwd: "/home/user/cdc/bin/controller",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "fork"
  },{
    name: "ChuckProtoParser",
    script: "ChuckProtoParser.dll",
    watch: true,
    cwd: "/home/user/cdc/bin/parser",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "fork"
  },{
    name: "DataConsumer",
    script: "DataConsumer.dll",
    watch: true,
    cwd: "/home/user/cdc/bin/consumer",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "cluster"
  },{
    name: "WebhookProcessor",
    script: "WebhookProcessor.dll",
    watch: true,
    cwd: "/home/user/cdc/bin/webhook",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "fork"
  }]
};