﻿using System;
using System.IO;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;
using UnityExtension;

namespace ModTools
{

    class GamePanelExtender : MonoBehaviour
    {


        private bool initializedZonedBuildingsPanel = false;
        private ZonedBuildingWorldInfoPanel zonedBuildingInfoPanel;
        private UILabel zonedBuildingAssetNameLabel;
        private UIButton zonedBuildingShowExplorerButton;
        private UIButton zonedBuildingDumpMeshTextureButton;

        private bool initializedServiceBuildingsPanel = false;
        private CityServiceWorldInfoPanel serviceBuildingInfoPanel;
        private UILabel serviceBuildingAssetNameLabel;
        private UIButton serviceBuildingShowExplorerButton;
        private UIButton serviceBuildingDumpMeshTextureButton;

        private UIView uiView;

        private SceneExplorer sceneExplorer;

        private ReferenceChain buildingsBufferRefChain;
        private ReferenceChain vehiclesBufferRefChain;
        private ReferenceChain vehiclesParkedBufferRefChain;

        private bool initializedCitizenVehiclePanel = false;
        private CitizenVehicleWorldInfoPanel citizenVehicleInfoPanel;
        private UILabel citizenVehicleAssetNameLabel;
        private UIButton citizenVehicleShowExplorerButton;
        private UIButton citizenVehicleDumpTextureMeshButton;

        private bool initializedCityServiceVehiclePanel = false;
        private CityServiceVehicleWorldInfoPanel cityServiceVehicleInfoPanel;
        private UILabel cityServiceVehicleAssetNameLabel;
        private UIButton cityServiceVehicleShowExplorerButton;
        private UIButton cityServiceVehicleDumpTextureMeshButton;


        private bool initializedPublicTransportVehiclePanel = false;
        private PublicTransportVehicleWorldInfoPanel publicTransportVehicleInfoPanel;
        private UILabel publicTransportVehicleAssetNameLabel;
        private UIButton publicTransportVehicleShowExplorerButton;
        private UIButton publicTransportVehicleDumpTextureMeshButton;


        void Awake()
        {
            uiView = FindObjectOfType<UIView>();
        }

        void OnDestroy()
        {
            try
            {
                Destroy(zonedBuildingAssetNameLabel.gameObject);
                Destroy(zonedBuildingShowExplorerButton.gameObject);
                Destroy(zonedBuildingDumpMeshTextureButton.gameObject);

                Destroy(serviceBuildingAssetNameLabel.gameObject);
                Destroy(serviceBuildingShowExplorerButton.gameObject);
                Destroy(serviceBuildingDumpMeshTextureButton.gameObject);


                zonedBuildingInfoPanel.component.Find<UILabel>("AllGood").isVisible = true;
                zonedBuildingInfoPanel.component.Find<UIPanel>("ProblemsPanel").isVisible = true;

                serviceBuildingInfoPanel.component.Find<UILabel>("AllGood").isVisible = true;
                serviceBuildingInfoPanel.component.Find<UIPanel>("ProblemsPanel").isVisible = true;

                Destroy(citizenVehicleAssetNameLabel.gameObject);
                Destroy(citizenVehicleShowExplorerButton.gameObject);
                Destroy(citizenVehicleDumpTextureMeshButton.gameObject);

                citizenVehicleInfoPanel.component.Find<UILabel>("Type").isVisible = true;

                Destroy(cityServiceVehicleAssetNameLabel.gameObject);
                Destroy(cityServiceVehicleShowExplorerButton.gameObject);
                Destroy(cityServiceVehicleDumpTextureMeshButton.gameObject);

                cityServiceVehicleInfoPanel.component.Find<UILabel>("Type").isVisible = true;

                Destroy(publicTransportVehicleAssetNameLabel.gameObject);
                Destroy(publicTransportVehicleShowExplorerButton.gameObject);
                Destroy(publicTransportVehicleDumpTextureMeshButton.gameObject);

                publicTransportVehicleInfoPanel.component.Find<UILabel>("Type").isVisible = true;
            }
            catch (Exception)
            {
            }
        }

        UIButton CreateButton(string text, int width, int height, UIComponent parentComponent, Vector3 offset, UIAlignAnchor anchor, MouseEventHandler handler)
        {
            var button = uiView.AddUIComponent(typeof(UIButton)) as UIButton;
            button.name = "ModTools Button";
            button.text = text;
            button.textScale = 0.8f;
            button.width = width;
            button.height = height;
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenu";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(255, 255, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.eventClick += handler;
            button.AlignTo(parentComponent, anchor);
            button.relativePosition += offset;
            return button;
        }

        UILabel CreateLabel(string text, int width, int height, UIComponent parentComponent, Vector3 offset,
            UIAlignAnchor anchor)
        {
            var label = uiView.AddUIComponent(typeof(UILabel)) as UILabel;
            label.text = text;
            label.name = "ModTools Label";
            label.width = width;
            label.height = height;
            label.textColor = new Color32(255, 255, 255, 255);
            label.AlignTo(parentComponent, anchor);
            label.relativePosition += offset;
            return label;
        }

        void AddBuildingPanelControls(WorldInfoPanel infoPanel, out UILabel assetNameLabel,
            out UIButton showExplorerButton, Vector3 showExplorerButtonOffset,
            out UIButton dumpMeshTextureButton, Vector3 dumpMeshTextureButtonOffset)
        {
            infoPanel.component.Find<UILabel>("AllGood").isVisible = false;
            infoPanel.component.Find<UIPanel>("ProblemsPanel").isVisible = false;

            assetNameLabel = CreateLabel
            (
                "AssetName: <>", 160, 24,
                infoPanel.component,
                new Vector3(8.0f, 48.0f, 0.0f),
                UIAlignAnchor.TopLeft
            );

            showExplorerButton = CreateButton
            (
                "Find in SceneExplorer", 160, 24,
                infoPanel.component,
                showExplorerButtonOffset,
                UIAlignAnchor.TopRight,
                (component, param) =>
                {
                    InstanceID instance = Util.GetPrivate<InstanceID>(infoPanel, "m_InstanceID");
                    sceneExplorer.ExpandFromRefChain(buildingsBufferRefChain.Add(instance.Building));
                    sceneExplorer.visible = true;
                }
            );

            dumpMeshTextureButton = CreateButton
            (
                "Dump mesh+texture", 160, 24,
                infoPanel.component,
                dumpMeshTextureButtonOffset,
                UIAlignAnchor.TopRight,
                (component, param) =>
                {
                    InstanceID instance = Util.GetPrivate<InstanceID>(infoPanel, "m_InstanceID");
                    var building = BuildingManager.instance.m_buildings.m_buffer[instance.Building];
                    var material = building.Info.m_material;
                    var mesh = building.Info.m_mesh;
                    var assetName = building.Info.name;

                    DumpAsset(assetName, mesh, material);
                }
            );
        }

        private static void DumpAsset(string assetName, Mesh mesh, Material material)
        {
            Log.Warning(String.Format("Dumping asset \"{0}\"", assetName));
            Util.DumpMeshToOBJ(mesh, String.Format("{0}.obj", assetName));
            Util.DumpTextureToPNG(material.GetTexture("_MainTex"), String.Format("{0}_MainTex.png", assetName));
            Util.DumpTextureToPNG(material.GetTexture("_XYSMap"), String.Format("{0}_xyz.png", assetName));
            Util.DumpTextureToPNG(material.GetTexture("_ACIMap"), String.Format("{0}_aci.png", assetName));
            Log.Warning("Done!");
        }

        void AddVehiclePanelControls(WorldInfoPanel infoPanel, out UILabel assetNameLabel, out UIButton showExplorerButton, out UIButton dumpMeshTextureButton)
        {
            infoPanel.component.Find<UILabel>("Type").isVisible = false;

            assetNameLabel = CreateLabel
            (
                "AssetName: <>", 160, 24,
                infoPanel.component,
                new Vector3(8.0f, 48.0f, 0.0f),
                UIAlignAnchor.TopLeft
            );

            showExplorerButton = CreateButton
            (
                "Find in SceneExplorer", 160, 24,
                infoPanel.component,
                new Vector3(-8.0f, -57.0f, 0.0f),
                UIAlignAnchor.BottomRight,
                (component, param) =>
                {
                    InstanceID instance = Util.GetPrivate<InstanceID>(infoPanel, "m_InstanceID");

                    if (instance.Vehicle == 0)
                    {
                        sceneExplorer.ExpandFromRefChain(vehiclesParkedBufferRefChain.Add(instance.ParkedVehicle));
                    }
                    else
                    {
                        sceneExplorer.ExpandFromRefChain(vehiclesBufferRefChain.Add(instance.Vehicle));
                    }

                    sceneExplorer.visible = true;
                }
            );

            dumpMeshTextureButton = CreateButton
            (
                "Dump mesh+texture", 160, 24,
                infoPanel.component,
                new Vector3(-8.0f, -25.0f, 0.0f),
                UIAlignAnchor.BottomRight,
                (component, param) =>
                {
                    InstanceID instance = Util.GetPrivate<InstanceID>(infoPanel, "m_InstanceID");
                    VehicleInfo vehicleInfo;
                    if (instance.Vehicle == 0)
                    {
                        vehicleInfo  = VehicleManager.instance.m_parkedVehicles.m_buffer[instance.ParkedVehicle].Info;
                    }
                    else
                    {
                        vehicleInfo = VehicleManager.instance.m_vehicles.m_buffer[instance.Vehicle].Info;
                    }
                    var material = vehicleInfo.m_material;
                    var mesh = vehicleInfo.m_mesh;
                    var assetName = vehicleInfo.name;

                    DumpAsset(assetName, mesh, material);
                }
            );
        }

        void Update()
        {
            if (!initializedZonedBuildingsPanel)
            {
                sceneExplorer = FindObjectOfType<SceneExplorer>();

                buildingsBufferRefChain = new ReferenceChain();
                buildingsBufferRefChain = buildingsBufferRefChain.Add(BuildingManager.instance.gameObject);
                buildingsBufferRefChain = buildingsBufferRefChain.Add(BuildingManager.instance);
                buildingsBufferRefChain = buildingsBufferRefChain.Add(typeof(BuildingManager).GetField("m_buildings"));
                buildingsBufferRefChain = buildingsBufferRefChain.Add(typeof(Array16<Building>).GetField("m_buffer"));

                zonedBuildingInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
                if (zonedBuildingInfoPanel != null)
                {
                    AddBuildingPanelControls(zonedBuildingInfoPanel, out zonedBuildingAssetNameLabel,
                        out zonedBuildingShowExplorerButton, new Vector3(-8.0f, 100.0f, 0.0f),
                        out zonedBuildingDumpMeshTextureButton, new Vector3(-8.0f, 132.0f, 0.0f)
                        );
                    initializedZonedBuildingsPanel = true;
                }
            }

            if (!initializedServiceBuildingsPanel)
            {
                sceneExplorer = FindObjectOfType<SceneExplorer>();
                serviceBuildingInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<CityServiceWorldInfoPanel>();
                if (serviceBuildingInfoPanel != null)
                {
                    AddBuildingPanelControls(serviceBuildingInfoPanel, out serviceBuildingAssetNameLabel,
                        out serviceBuildingShowExplorerButton, new Vector3(-8.0f, 100.0f, 0.0f),
                        out serviceBuildingDumpMeshTextureButton, new Vector3(-8.0f, 132.0f, 0.0f)
                        );
                    initializedServiceBuildingsPanel = true;
                }
            }

            if (!initializedCitizenVehiclePanel)
            {
                sceneExplorer = FindObjectOfType<SceneExplorer>();

                vehiclesBufferRefChain = new ReferenceChain();
                vehiclesBufferRefChain = vehiclesBufferRefChain.Add(VehicleManager.instance.gameObject);
                vehiclesBufferRefChain = vehiclesBufferRefChain.Add(VehicleManager.instance);
                vehiclesBufferRefChain = vehiclesBufferRefChain.Add(typeof(VehicleManager).GetField("m_vehicles"));
                vehiclesBufferRefChain = vehiclesBufferRefChain.Add(typeof(Array16<Vehicle>).GetField("m_buffer"));

                vehiclesParkedBufferRefChain = new ReferenceChain();
                vehiclesParkedBufferRefChain = vehiclesParkedBufferRefChain.Add(VehicleManager.instance.gameObject);
                vehiclesParkedBufferRefChain = vehiclesParkedBufferRefChain.Add(VehicleManager.instance);
                vehiclesParkedBufferRefChain = vehiclesParkedBufferRefChain.Add(typeof(VehicleManager).GetField("m_parkedVehicles"));
                vehiclesParkedBufferRefChain = vehiclesParkedBufferRefChain.Add(typeof(Array16<VehicleParked>).GetField("m_buffer"));

                citizenVehicleInfoPanel = GameObject.Find("(Library) CitizenVehicleWorldInfoPanel").GetComponent<CitizenVehicleWorldInfoPanel>();
                if (citizenVehicleInfoPanel != null)
                {
                    AddVehiclePanelControls(
                        citizenVehicleInfoPanel,
                        out citizenVehicleAssetNameLabel,
                        out citizenVehicleShowExplorerButton,
                        out citizenVehicleDumpTextureMeshButton
                        );
                    initializedCitizenVehiclePanel = true;
                }
            }

            if (!initializedCityServiceVehiclePanel)
            {
                sceneExplorer = FindObjectOfType<SceneExplorer>();

                cityServiceVehicleInfoPanel = GameObject.Find("(Library) CityServiceVehicleWorldInfoPanel").GetComponent<CityServiceVehicleWorldInfoPanel>();
                if (cityServiceVehicleInfoPanel != null)
                {
                    AddVehiclePanelControls(
                        cityServiceVehicleInfoPanel,
                        out cityServiceVehicleAssetNameLabel,
                        out cityServiceVehicleShowExplorerButton,
                        out cityServiceVehicleDumpTextureMeshButton);
                    initializedCityServiceVehiclePanel = true;
                }
            }

            if (!initializedPublicTransportVehiclePanel)
            {
                sceneExplorer = FindObjectOfType<SceneExplorer>();

                publicTransportVehicleInfoPanel = GameObject.Find("(Library) PublicTransportVehicleWorldInfoPanel").GetComponent<PublicTransportVehicleWorldInfoPanel>();
                if (publicTransportVehicleInfoPanel != null)
                {
                    AddVehiclePanelControls(
                        publicTransportVehicleInfoPanel,
                        out publicTransportVehicleAssetNameLabel,
                        out publicTransportVehicleShowExplorerButton,
                        out publicTransportVehicleDumpTextureMeshButton);
                    initializedPublicTransportVehiclePanel = true;
                }
            }

            if (zonedBuildingInfoPanel.component.isVisible)
            {
                InstanceID instance = Util.GetPrivate<InstanceID>(zonedBuildingInfoPanel, "m_InstanceID");
                var building = BuildingManager.instance.m_buildings.m_buffer[instance.Building];
                zonedBuildingAssetNameLabel.text = "AssetName: " + building.Info.name;
            }

            if (serviceBuildingInfoPanel.component.isVisible)
            {
                InstanceID instance = Util.GetPrivate<InstanceID>(serviceBuildingInfoPanel, "m_InstanceID");
                var building = BuildingManager.instance.m_buildings.m_buffer[instance.Building];
                serviceBuildingAssetNameLabel.text = "AssetName: " + building.Info.name;
            }

            if (citizenVehicleInfoPanel.component.isVisible)
            {
                InstanceID instance = Util.GetPrivate<InstanceID>(citizenVehicleInfoPanel, "m_InstanceID");

                if (instance.Vehicle == 0)
                {
                    var vehicle = VehicleManager.instance.m_parkedVehicles.m_buffer[instance.ParkedVehicle];
                    citizenVehicleAssetNameLabel.text = "AssetName: " + vehicle.Info.name;
                }
                else
                {
                    var vehicle = VehicleManager.instance.m_vehicles.m_buffer[instance.Vehicle];
                    citizenVehicleAssetNameLabel.text = "AssetName: " + vehicle.Info.name;
                }
            }

            if (cityServiceVehicleInfoPanel.component.isVisible)
            {
                InstanceID instance = Util.GetPrivate<InstanceID>(cityServiceVehicleInfoPanel, "m_InstanceID");

                if (instance.Vehicle == 0)
                {
                    var vehicle = VehicleManager.instance.m_parkedVehicles.m_buffer[instance.ParkedVehicle];
                    cityServiceVehicleAssetNameLabel.text = "AssetName: " + vehicle.Info.name;
                }
                else
                {
                    var vehicle = VehicleManager.instance.m_vehicles.m_buffer[instance.Vehicle];
                    cityServiceVehicleAssetNameLabel.text = "AssetName: " + vehicle.Info.name;
                }
            }

            if (publicTransportVehicleInfoPanel.component.isVisible)
            {
                InstanceID instance = Util.GetPrivate<InstanceID>(publicTransportVehicleInfoPanel, "m_InstanceID");

                if (instance.Vehicle == 0)
                {
                    var vehicle = VehicleManager.instance.m_parkedVehicles.m_buffer[instance.ParkedVehicle];
                    publicTransportVehicleAssetNameLabel.text = "AssetName: " + vehicle.Info.name;
                }
                else
                {
                    var vehicle = VehicleManager.instance.m_vehicles.m_buffer[instance.Vehicle];
                    publicTransportVehicleAssetNameLabel.text = "AssetName: " + vehicle.Info.name;
                }
            }
        }
    }
}