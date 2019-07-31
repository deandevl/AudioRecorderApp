<template>
  <div class=audioRecorder>
    <tab-comp :tab_routes="tab_routes">
      <div class="audioRecorder_titleBox" slot="header">
        <div class="audioRecorder_title">Audio Recorder</div>
        <div class="audioRecorder_author">Built at <a href="https://github.com/deandevl">deandevl</a> </div>
      </div>
    </tab-comp>
  </div>
</template>

<script>
  import Vue from 'vue';
  import TabComp from 'tabcomp';
  import VueRouter from 'vue-router';
  import {tab_routes} from './tab_routes';

  Vue.use(VueRouter);
  const router = new VueRouter({
    routes: tab_routes
  });
  
  export default {
    name: "App",
    router: router,
    data() {
      return {
        tab_routes: tab_routes
      };
    },
    components: {
      TabComp
    }
  }
</script>

<style lang="less">
  .audioRecorder {
    display: flex;
    flex-direction: column;
    max-height: 50rem;
    background: black;
    font-family: Verdana,serif;

    &_titleBox {
      padding: .8rem;
      background: linear-gradient(to bottom, #1e5799 0%,#207cca 53%,#7db9e8 100%);
      height: 5rem;
    }

    &_title {
      font: normal normal bold 42px/1 Arial Black, Gadget, sans-serif;
      text-shadow: 4px 0 9px rgba(86,170,255,0.9), 4px 0 9px rgba(86,170,255,0.9);
      font-size:2.75rem;
    }

    &_author {
      margin-top: 1rem;
      margin-left: 16rem;
      font: normal normal bold 12px/1 Arial Black, Gadget, sans-serif;
      text-shadow: 4px 0 9px rgba(86,170,255,0.9), 4px 0 9px rgba(86,170,255,0.9);
    }
  }
</style>