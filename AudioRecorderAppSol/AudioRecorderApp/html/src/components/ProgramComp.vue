<template>
  <div class="programComp">

    <section class="programComp_programsSec">
      <select-comp
          class="programComp_selectGroup"
          heading="Groups"
          placeholder="Select a group"
          :items="group_names"
          :select_value="group_name"
          :single_border="single_border"
          :css_variables="select_css"
          v-on:select_comp_value_changed="group_changed">
      </select-comp>

      <table-comp
          class="programComp_tableComp"
          title="Programs"
          :table_height="table_height"
          :rows="table_rows"
          :headings="table_headings"
          :column_widths="table_column_widths"
          :css_variables="table_css"
          v-on:table_comp_row="table_row_changed">
      </table-comp>
    </section>

    <section class="programComp_inputSec">
      <input-comp
          heading="program"
          placeholder="Enter program"
          header_position="above"
          :input_value="input_program_name"
          input_size="20"
          :single_border="single_border"
          :css_variables="input_css"
          v-on:input_comp_value_changed="value => {this.input_program_name = value}">
      </input-comp>
      <input-comp
          heading="group"
          placeholder="Enter group"
          header_position="above"
          :input_value="input_group_name"
          input_size="20"
          :single_border="single_border"
          :css_variables="input_css"
          v-on:input_comp_value_changed="value => {this.input_group_name = value}">
      </input-comp>
      <input-comp
          heading="start"
          placeholder="Enter start"
          input_size="10"
          header_position="above"
          :input_value="input_program_start"
          :single_border="single_border"
          :css_variables="input_css"
          v-on:input_comp_value_changed="value => {this.input_program_start = value}">
      </input-comp>
      <input-comp
          heading="stop"
          placeholder="Enter stop"
          input_size="10"
          header_position="above"
          :input_value="input_program_stop"
          :single_border="single_border"
          :css_variables="input_css"
          v-on:input_comp_value_changed="value => {this.input_program_stop = value}">
      </input-comp>
      <input-comp
          heading="url"
          header_position="above"
          placeholder="Enter url"
          input_size="40"
          :input_value="input_program_url"
          :single_border="single_border"
          :css_variables="input_css"
          v-on:input_comp_value_changed="value => {this.input_program_url = value}">
      </input-comp>
      <checker-comp
          class=programComp_checkerComp
          heading="Days of the Week"
          drop_panel_height="100px"
          :items="dow_items"
          :blur_panel="blur_panel"
          :css_variables="dow_input_css">
      </checker-comp>
    </section>

    <section class="programComp_program_button_sec">
      <button-comp
          :css_variables="button_css"
          v-on:button_comp_clicked="add_program_clicked">Add
      </button-comp>
      <button-comp
          :css_variables="button_css"
          v-on:button_comp_clicked="update_program_clicked">Update
      </button-comp>
      <button-comp
          :css_variables="button_css"
          v-on:button_comp_clicked="delete_program_clicked">Delete
      </button-comp>
      <button-comp
          :css_variables="button_css"
          v-on:button_comp_clicked="cancel_program">Cancel
      </button-comp>
    </section>
    <section class="programComp_start_stop_sec">
      <div class="programComp_status">{{status_content}}</div>
      <progress-comp
          :percent="progress_percent"
          :css_variables="progress_css">
      </progress-comp>
      <section class="programComp_start_stop_buttons_sec">
        <button-comp
            :css_variables="button_css"
            v-on:button_comp_clicked="start_recording">Start Recordings
        </button-comp>
        <button-comp
            :css_variables="button_css"
            v-on:button_comp_clicked="cancel_recording">Cancel Recordings
        </button-comp>
      </section>
    </section>
  </div>
</template>

<script>
  import ButtonComp from 'buttoncomp';
  import CheckerComp from 'checkercomp';
  import InputComp from 'inputcomp';
  import ProgressComp from 'progresscomp';
  import SelectComp from 'selectcomp';
  import TableComp from 'tablecomp';
  
  export default {
    name: "ProgramComp",
    data() {
      return {
        localhost: 'http://localhost:8081/audio',
        websocket: null,
        status_content: "Status",
        progress_percent: null,
        crud_action: null,
        group_names: null,
        group_name: null,
        program_names: null,

        programs_dow: null,
        backup_program: null,
        current_table_row: null,

        //inputs
        input_program_name: null,
        input_group_name: null,
        input_program_start: null,
        input_program_stop: null,
        input_program_url: null,

        button_css: {
          button_comp_font_size: ".6rem"
        },

        select_css: {
          select_comp_heading_color: "white",
          select_comp_color: "white",
          select_comp_arrow_color: "white",
          select_comp_border_color: "white",
          select_comp_items_panel_position: "absolute",
          select_comp_items_panel_z_index: "100",
          select_comp_items_panel_color: "white",
          select_comp_items_panel_background: "#2E1076",
          select_comp_items_panel_border: "1px solid white",
          select_comp_item_hover_color: "gold"
        },

        table_rows: null,
        table_headings: ['program', 'groupname', 'start', 'stop', 'stationurl', 'status'],
        table_height: 200,
        table_column_widths: [150, 120, 40, 40, 340, 80],
        table_css: {
          table_comp_title_color: "white",
          table_comp_thead_color: "white",
          table_comp_thead_border_bottom: "1px solid white",
          table_comp_thead_background: "transparent",
          table_comp_tbody_height: "220px",
          table_comp_row_color: "white",
          table_comp_row_odd_background: "transparent",
          table_comp_row_even_background: "transparent",
          table_comp_row_border_bottom: "1px solid white",
        },

        input_css: {
          input_comp_heading_color: "white",
          input_comp_input_color: "white",
          input_comp_input_border_color: "white",
          input_comp_input_placeholder_color: "white",
          input_comp_input_focus_outline: "gold",
          input_comp_input_focus_background: "transparent"
        },

        dow_items: [
          {text: 'Sunday', checked: false},
          {text: 'Monday', checked: false},
          {text: 'Tuesday', checked: false},
          {text: 'Wednesday', checked: false},
          {text: 'Thursday', checked: false},
          {text: 'Friday', checked: false},
          {text: 'Saturday', checked: false}
        ],
        dow_input_css: {
          checker_comp_heading_color: "white",
          checker_comp_icon_color: "white",
          checker_comp_items_panel_position: "absolute",
          checker_comp_items_panel_z_index: "100",
          checker_comp_items_panel_color: "white",
          checker_comp_items_panel_background: "transparent",
          checker_comp_items_panel_border: "1px solid white",
          checker_comp_checkbox_border: "1px solid white",
          checker_comp_notchecked_background: "transparent"
        },

        progress_css: {
          progress_comp_track_border: "1px solid white",
          progress_comp_heading_color: "white"
        },

        blur_panel: false,
        single_border: true
      }
    },
    components: {
      ButtonComp,
      CheckerComp,
      InputComp,
      ProgressComp,
      SelectComp,
      TableComp
    },
    mounted() {
      //get group names and the group name from the last session
      const self = this;
      async function get_groups_session() {
        try {
          let url = self.localhost;
          let request_data = {
            action: 'getGroupNames'
          };
          let request_data_str = JSON.stringify(request_data);
          let config={
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          let response = await fetch(url, config);
          let resp_str = await response.text();
          //a string array of group names
          self.group_names = JSON.parse(resp_str);

          //get the last session's group name
          url = self.localhost;
          request_data = {
            action: 'getSession'
          };
          request_data_str = JSON.stringify(request_data);
          config = {
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          response = await fetch(url, config);
          if(response.ok){
            //set the select component's group name from last session
            self.group_name = await response.text();
          }else {
            this.status_content =`Getting group names/session error: ${response.statusText}`;
          }
        }catch(error) {
          this.status_content =`Getting group names/session error: ${error.message}`;
        }
      }
      get_groups_session();

      //setup websocket events
      this.websocket = new WebSocket("ws://localhost:54002/audio");
      this.websocket.addEventListener('open', () => {
        this.status_content = "Websocket is open.";
      });
      this.websocket.addEventListener('close', () => {
        this.status_content = "Websocket is closed.";
      });
      this.websocket.addEventListener('error', () => {
        this.status_content = "Websocket error.";
      });
      this.websocket.addEventListener('message', (e) => {
        const message_obj = JSON.parse(e.data);
        const action = message_obj.action;
        let row_idx = null;
        switch(action) {
          case 'startProcessing':
            this.status_content = message_obj.message;
            break;
          case 'waiting':
            row_idx = this.program_names.indexOf(message_obj.message);
            this.update_table_cell(row_idx,'waiting','yellow_color');
            break;
          case 'startRecord':
            row_idx = this.program_names.indexOf(message_obj.message);
            this.update_table_cell(row_idx,'writing','orange_color');
            this.progress_percent = 0;
            this.status_content = "Starting " + message_obj.message;
            break;
          case 'progressRecord':
            const interval_cnt = message_obj.message;
            this.progress_percent = interval_cnt*10;
            break;
          case 'endRecord':
            row_idx = this.program_names.indexOf(message_obj.message);
            this.update_table_cell(row_idx,'completed','red_color');
            this.status_content = "Ending " + message_obj.message;
            this.progress_percent = 100;
            break;
          case 'endProcessing':
            this.status_content = message_obj.message;
            this.progress_percent = 0;
            for(let i = 0; i < this.program_names.length; i++){
              this.update_table_cell(i,' ','white_color');
            }
            break;
          case 'cancelProcessing':
            this.status_content = message_obj.message;
            this.progress_percent = 0;
            for(let i = 0; i < this.program_names.length; i++){
              this.update_table_cell(i,' ','white_color');
            }
            break;
          case 'error':
            this.status_content = message_obj.message;
            for(let i = 0; i < this.program_names.length; i++){
              this.update_table_cell(i,' ','white_color');
            }
            break;
        }
      })
    },
    methods: {
      update_table_cell: function(row_idx,cell_value,cell_class){
        //change cell value/color
        this.table_rows[row_idx].splice(5,1,[cell_value,cell_class]);
      },
      group_changed: function(value) {
        if(value !== null) {
          this.group_name = value;
          this.get_programs();
        }
      },
      get_programs: function() {
        if(this.group_name !== null) {
          const url=this.localhost;
          const request_data={
            action: 'getPrograms',
            GroupName: this.group_name
          };
          const request_data_str=JSON.stringify(request_data);
          const config={
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          fetch(url, config).then(response => {
            if(response.ok) {
              return response.text();
            }
            throw new Error(response.statusText);
          }).then(resp_str => {
            const programs = JSON.parse(resp_str); //programs is an array of program objects
            this.table_rows = [];
            this.programs_dow = {};
            this.program_names = [];

            programs.forEach((program) => { //each program is an object
              const row = [];
              row.push([program.Name,'']);
              row.push([program.GroupName,'']);
              row.push([program.Start,'']);
              row.push([program.Stop,'']);
              row.push([program.Url,'']);
              row.push([' ','']); //for status column
              this.table_rows.push(row);
              this.programs_dow[program.Name]=program.Dow;
              this.program_names.push(program.Name);
            });
          }).catch(error => {
            this.status_content = `Getting programs error: ${error.message}`;
          })
        }
      },
      table_row_changed: function(obj) {
        this.current_table_row = obj.row_values;

        //set inputs
        this.input_program_name = obj.row_values[0];
        this.input_group_name = obj.row_values[1];
        this.input_program_start = obj.row_values[2];
        this.input_program_stop = obj.row_values[3];
        this.input_program_url = obj.row_values[4];

        //set checker for days of week
        const dow = this.programs_dow[obj.row_values[0]];
        for(let i = 0; i < this.dow_items.length; i++){
          if(dow.indexOf(i) !== -1) {
            this.dow_items[i].checked = true;
          }else{
            this.dow_items[i].checked = false;
          }
        }
      },
      add_program_clicked: function(){
        //get dow indexes for program
        const dow_indices = [];
        for(const [index,item] of this.dow_items.entries()){
          if(item.checked){
            dow_indices.push(index);
          }
        }
        const program = {
          Name: this.input_program_name,
          GroupName: this.input_group_name,
          Start: parseInt(this.input_program_start),
          Stop: parseInt(this.input_program_stop),
          Url: this.input_program_url,
          Dow: dow_indices
        };
        this.add_program(program,false);
      },
      add_program: function(program,isCancel) {
        //has days of the week been checked
        if(program.Dow.length > 0) {
          this.crud_action = 'add';
          const url = this.localhost;
          const request_data = {
            action: 'addProgram',
            program: program
          };
          const request_data_str = JSON.stringify(request_data);
          const config = {
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          fetch(url, config).then(response => {
            if(response.ok) {
              return response.text();
            }
            throw new Error(response.statusText);
          }).then(resp_str => {
            //returns both a list of group names and backup program
            const program_obj = JSON.parse(resp_str);
            this.group_names = program_obj.group_names;
            this.group_name = this.input_group_name;
            this.backup_program = JSON.parse(JSON.stringify(program_obj.backup_program));

            //reset table
            this.get_programs();

            //send out status message
            if(isCancel) {
              this.status_content = `Successfully cancelled delete ${program.Name}`;
            } else {
              this.status_content = `Successfully added ${program.Name}`;
            }
          }).catch(error => {
            this.status_content = `Add program error: ${error.message}`;
          })
        } else {
          this.status_content = 'Days of the Week has not been checked';
        }
      },
      delete_program_clicked: function(){
        //get dow indexes for program
        const dow_indices = [];
        for(const [index,item] of this.dow_items.entries()){
          if(item.checked){
            dow_indices.push(index);
          }
        }
        const program = {
          Name: this.input_program_name,
          GroupName: this.input_group_name,
          Start: parseInt(this.input_program_start),
          Stop: parseInt(this.input_program_stop),
          Url: this.input_program_url,
          Dow: dow_indices
        };
        this.delete_program(program,false);
      },
      delete_program: function(program,isCancel) {
        this.crud_action = 'delete';

        const url = this.localhost;
        const request_data = {
          action: 'deleteProgram',
          program: program
        };
        const request_data_str = JSON.stringify(request_data);
        const config = {
          method: 'POST',
          mode: 'cors',
          body: request_data_str,
          headers: new Headers({
            'Content-Type': 'application/json',
            'Content-Length': request_data_str.length
          })
        };
        fetch(url, config).then(response => {
          if(response.ok){
            return response.text();
          }
          throw new Error(response.statusText);
        }).then(resp_str => {
          //returns a list of group names and backup program
          const program_obj = JSON.parse(resp_str);
          this.group_names = program_obj.group_names;
          this.backup_program = JSON.parse(JSON.stringify(program_obj.backup_program));

          //reset table
          this.group_name = program.GroupName;
          if(this.group_names.length > 0 && this.group_names.indexOf(this.group_name) === -1) {
            this.group_name = null;
            this.table_rows = [];
          }else {
            this.get_programs();
          }

          if(isCancel) {
            this.status_content = `Successfully cancelled add ${program.Name}`;
          } else {
            this.status_content = `Successfully deleted ${program.Name}`;
          }
        }).catch(error => {
          this.status_content = `Delete program error: ${error.message}`;
        })
      },
      update_program_clicked: function(){
        //has days of the week been checked
        const dow_indices = [];
        for(const [index,item] of this.dow_items.entries()){
          if(item.checked){
            dow_indices.push(index);
          }
        }
        const program={};
        program.currentGroupName = this.current_table_row[1];
        program.updateGroupName = this.input_group_name;
        program.currentName = this.current_table_row[0];
        program.updateName = this.input_program_name;
        program.Start = parseInt(this.input_program_start);
        program.Stop = parseInt(this.input_program_stop);
        program.Url = this.input_program_url;
        program.Dow = dow_indices;

        this.update_program(program,false);
      },
      update_program: function(program, isCancel) {
        if(program.Dow.length > 0) {
          this.crud_action = 'update';
          const url = this.localhost;
          const request_data = {
            action: 'updateProgram',
            program: program
          };
          const request_data_str = JSON.stringify(request_data);
          const config = {
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          fetch(url, config).then(response => {
            if(response.ok){
              return response.text();
            }
            throw new Error(response.statusText);
          }).then(resp_str => {
            //returns a list of group names and backup program
            const program_obj = JSON.parse(resp_str);
            this.group_names = program_obj.group_names;
            this.backup_program = JSON.parse(JSON.stringify(program_obj.backup_program));
            this.group_name = program.updateGroupName;

            //reset table
            if(this.group_names.length > 0 && this.group_names.indexOf(this.group_name) === -1) {
              this.group_name = null;
              this.table_rows = [];
            }else {
              this.get_programs();
            }

            //send out status message
            if(isCancel) {
              this.status_content = `Successfully cancelled update ${program.currentName}`;
            } else {
              this.status_content = `Successfully updated ${program.currentName}`;
            }
          }).catch(error => {
            this.status_content = `Update program error: ${error.message}`;
          })
        } else {
          this.status_content = 'Days of the Week has not been checked';
        }
      },
      cancel_program: function() {
        if(this.backup_program !== null) {
          switch(this.crud_action) {
            case 'add':
              this.delete_program(this.backup_program,true);
              break;
            case 'delete':
              this.add_program(this.backup_program, true);
              break;
            case 'update':
              const program={};
              program.currentGroupName = this.input_group_name;
              program.updateGroupName = this.backup_program.GroupName;
              program.currentName = this.input_program_name;
              program.updateName = this.backup_program.Name;
              program.Start = this.backup_program.Start;
              program.Stop = this.backup_program.Stop;
              program.Url = this.backup_program.Url;
              program.Dow = this.backup_program.Dow;

              this.update_program(program, true);
              break;
          }
        }
      },
      start_recording: function() {
        try {
          //update AudioSession of database with current group name
          const url=this.localhost;
          let request_data = {
            action: 'setSession',
            GroupName: this.group_name
          };
          let request_data_str=JSON.stringify(request_data);
          const config={
            method: 'POST',
            mode: 'cors',
            body: request_data_str,
            headers: new Headers({
              'Content-Type': 'application/json',
              'Content-Length': request_data_str.length
            })
          };
          fetch(url, config).then(response => {
            if(response.ok){
              return response.text();
            }
            throw new Error(response.statusText);
          }).then(resp_str => {
            this.status_content = resp_str;
          }).catch(error => {
            this.status_content = `Setting session error: ${error.message}`;
          });

          //send start message with selected grooup name via server's websocket channel
          //server will start processing/recording programs in the group
          request_data = {
            action: 'startRecording',
            GroupName: this.group_name
          };
          request_data_str = JSON.stringify(request_data);
          this.websocket.send(request_data_str);
        }catch(ex){
          this.status_content = `Start recording error: ${ex.message}`;
        }
      },
      cancel_recording: function() {
        //send start message via server's websocket channel that we want to cancel all recordings
        try{
          const request_data = {
            action: 'cancelProcessing'
          };
          const request_data_str = JSON.stringify(request_data);
          this.websocket.send(request_data_str);
        }catch(ex){
          this.status_content = `Cancel recordings error: ${ex.message}`;
        }
      }
    }
  }
</script>

<style lang="less">
  .white_color {
    color: white;
  }
  .red_color {
    color: red;
  }
  .orange_color {
    color: orange;
  }
  .yellow_color {
    color: yellow;
  }
  .programComp {
    display: flex;
    flex-direction: column;
    width: 100%;
    height: 100%;
    font-family: Verdana,serif;
    padding: 1rem;

    &_programsSec {
      display: flex;
      flex-direction: row;
      align-self: center;
    }

    &_selectGroup {
      margin-top: 20px;
    }

    &_tableComp {
      margin-left: 30px;
    }

    &_inputSec {
      display: flex;
      flex-direction: row;
      justify-content:space-between;
      width: 80rem;
      margin-top: 4rem;
      align-self: center;
    }

    &_checkerComp {
      margin-left: 90px;
    }
    
    &_program_button_sec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      width: 25rem;
      margin-top: 2.5rem;
      align-self: center;
    }

    &_start_stop_sec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      width: 70rem;
      margin-top: 2rem;
      align-self: center;
    }

    &_status {
      color: white;
      font-size: 1rem;
      max-width: 16rem;
    }

    &_start_stop_buttons_sec {
      display: flex;
      flex-direction: row;
      justify-content: space-between;
      width: 15rem;
    }
  }
</style>