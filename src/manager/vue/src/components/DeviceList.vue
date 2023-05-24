<template>
    <div>
      <ul>
        <li v-for="device in devices" :key="device.id">
          {{ device.name }}
        </li>
      </ul>
    </div>
  </template>
  
  <script>
  export default {
    name: 'DeviceList',
    created() {
      this.$store.dispatch('fetchDevices');
    },
    computed: {
      devices() {
        return this.$store.state.devices;
      },
    },
  };
  </script>
  
  <style scoped>
  /* You can add some styles here */
  </style>
  