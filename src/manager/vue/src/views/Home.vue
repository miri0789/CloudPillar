<template>
  <div class="home">
    <h1>Welcome to Our IoT Management Dashboard</h1>
    <p>This is a single-page application for managing IoT assets.</p>
    <!-- You might include some components here. For example, a list of devices -->
    <!-- <DeviceList /> -->
  </div>
</template>

<script>
// You might import some components here. For example, a list of devices
import DeviceList from '@/components/DeviceList.vue';

export default {
  name: 'Home',
  components: {
    // Here, you'd add any components that you imported
    DeviceList,
  },
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
h1, p {
  text-align: center;
}
</style>
