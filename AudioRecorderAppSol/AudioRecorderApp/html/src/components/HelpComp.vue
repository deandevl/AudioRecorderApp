<template>
  <div class="helpComp">
    <p>
      To use audio recorder you will need five pieces of information on the live audio stream
      program that you want to record:
    </p>
    <p class="subpar">
      1.  Get the url for the live audio stream (example: http://108.163.223.242:8003/stream).<br>
      2.  Determine the start and stop times for the audio program in 24 hour format. (i.e. 1:00 pm is 1300)<br>
      3.  Determine the days of the week that the program is broadcast.<br>
      4.  Think of a short title or name to be associated with the program.<br>
      5.  Think of a short group name.  Group names are used to consolidate similar programs.
    </p>
    <p>
      Enter all of this information in the edit boxes of the application then click the
      'Add' button.  The entered information will be entered into the program's database.  If you make a mistake
      after the addition, simply click the 'Cancel' button and start again.  To delete, select a program from the table
      and click the 'Delete' button.  The delete action can also be cancelled by clicking the 'Cancel' button.  To
      modify a selected program, make changes in the edit boxes then click the 'Update' button.  The update action can
      also be cancelled.

      To start recording the audio click the 'Start Recordings' button. All programs with start times equal
      to or greater than the current time and with the current day of the week will be scheduled to be recorded.
      The resulting .mp3 audio files are archived in your c:\temp folder.
    </p>
  </div>
</template>

<script>
  export default {
    name: "HelpComp",
  }
</script>

<style>
  .helpComp {
    width: 800px;
    height: 100%;
    font-family: Verdana,serif;
    padding: 1rem;
    background-color: black;
    color: white;
  }
</style>