## Water Sports VR Simulator
This is a course project for VR/AR and Game Design, 2024, SJTU

In this project, we implemented a VR simulator for simulating water sports. Users can choose to operate a kayak or electric yacht in an immersive way and experience water sports on a realistic and interactive sea surface.

The scene construction was implemented by [33chilsome](https://github.com/33chilsome). The simulation of kayak and electric yacht with buoyancy, as well as the VR implementation, were implemented by [ksmgc1](https://github.com/ksmgc1). The simulation and interactive effects of the ocean surface were implemented by me.

<figure>
<p align="center" width="100%">
  <img src="https://github.com/user-attachments/assets/c17fd968-ed23-4d37-8ddb-41c718f0107b" alt="image" style="width:80%">
  <p align="center" width="100%"><text align="text:center">Project Preview</text></p>
  </p>
</figure>

For the ocean simulation, I implemented surface waves based on FFT and Phillips spectrum, and used extrusion to create a more refined foam effect. For interactivity, I achieved the interaction between objects and the ocean surface by solving the NS equations.

<figure>
<p align="center" width="100%">
  <img src="https://github.com/user-attachments/assets/9e8dae24-caa1-4d4e-990d-6eca43402707" alt="image" style="width:80%">
  <p align="center" width="100%"><text align="text:center">Waves</text></p>
  </p>
</figure>

<figure>
<p align="center" width="100%">
  <img src="https://github.com/user-attachments/assets/c6f54b21-52bb-4ce3-849c-3357c4aff800" alt="image" style="width:80%">
  <p align="center" width="100%"><text align="text:center">Interaction and Buoyancy</text></p>
  </p>
</figure>
