import ProgramComp from './components/ProgramComp.vue';
import HelpComp from './components/HelpComp.vue';
import AboutComp from './components/AboutComp.vue';

export const tab_routes = [
  {path: '/',component: ProgramComp,name:'Programs'},
  {path: '/help',component: HelpComp,name:'Help'},
  {path: '/about',component: AboutComp,name:'About'}
];