module.exports = {
  apps: [{
    name: "ChuckDeviceController",
    script: "ChuckDeviceController.dll",
    watch: true,
    cwd: "/home/user/cdc/bin",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "fork"
  },{
    name: "DataConsumer",
    script: "DataConsumer.dll",
    watch: true,
    cwd: "/home/user/cdc/bin",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "cluster"
  },{
    name: "WebhookProcessor",
    script: "WebhookProcessor.dll",
    watch: true,
    cwd: "/home/user/cdc/bin",
    interpreter: "dotnet",
    instances: 1,
    exec_mode: "fork"
  }]
};