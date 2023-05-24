import Vue from 'vue'
import Vuex from 'vuex'

Vue.use(Vuex)

export default new Vuex.Store({
  state: {
    devices: [],
  },
  mutations: {
    setDevices(state, devices) {
      state.devices = devices;
    },
  },
  actions: {
    async fetchDevices({ commit }) {
      const response = await axios.get('/api/devices');
      commit('setDevices', response.data);
    },
  },
})
