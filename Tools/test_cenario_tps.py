"""Regression checks for floating props and inverted signs. Run from the project root."""
import json
import math
from pathlib import Path
import re
import struct
import unittest
import numpy as np
import yaml
from derby_scene_geometry import mesh_data, pitch_corners, trans

ROOT = Path('Assets/DerbySegundoTempo/Cenarios/Estadio')
SCENE = Path('Assets/DerbySegundoTempo/Cenas/Fases/Estadio_TPS_Backup_Visual.unity')


def documents(path):
    data = re.sub(r'^%.*\n', '', path.read_text(), flags=re.M)
    data = re.sub(r'^--- !u!\d+ &-?\d+(?: stripped)?', '---', data, flags=re.M)
    return list(yaml.safe_load_all(data))


class VisualSceneChecks(unittest.TestCase):
    def test_ground_contact_uses_pitch_triangles(self):
        vertices, faces, _ = mesh_data(16)
        triangles = vertices[faces]
        a = triangles[:, 0][:, [0, 2]]
        e = triangles[:, 1][:, [0, 2]] - a
        f = triangles[:, 2][:, [0, 2]] - a
        den = e[:, 0] * f[:, 1] - e[:, 1] * f[:, 0]
        den = np.where(abs(den) < 1e-8, np.nan, den)
        for item in json.loads((ROOT / 'posicionamento.json').read_text()):
            x, y, z = item['position']
            if item['prefab'] == 'BolaFutebol':
                y -= .285  # Centro da esfera: raio + pequena folga inicial acima do gramado.
            delta = np.array([x, z]) - a
            u = (delta[:, 0] * f[:, 1] - delta[:, 1] * f[:, 0]) / den
            v = (e[:, 0] * delta[:, 1] - e[:, 1] * delta[:, 0]) / den
            levels = triangles[:, 0, 1] + u * (triangles[:, 1, 1] - triangles[:, 0, 1]) + v * (triangles[:, 2, 1] - triangles[:, 0, 1])
            hit = (u >= -1e-5) & (v >= -1e-5) & (u + v <= 1.00001)
            self.assertTrue(np.any(abs(levels[hit] - y) < .001), item)

    def test_props_have_grounded_supports(self):
        for path in (ROOT / 'Prefabs').glob('*.prefab'):
            if path.stem == 'BolaFutebol':
                continue  # Esfera apoiada pelo raio, em vez de pes decorativos.
            nodes = documents(path)
            transforms = [n['Transform'] for n in nodes if 'Transform' in n]
            visible_bottoms = []
            for transform in transforms[1:]:
                y = transform['m_LocalPosition']['y']
                size = transform['m_LocalScale']['y']
                visible_bottoms.append(y - size / 2)
            if visible_bottoms:
                self.assertAlmostEqual(min(visible_bottoms), 0, places=4, msg=str(path))
            else:
                collider = next(n['BoxCollider'] for n in nodes if 'BoxCollider' in n)
                bottom = collider['m_Center']['y'] - collider['m_Size']['y'] / 2
                self.assertLess(abs(bottom), .02, str(path))

    def test_sign_uvs_and_front_winding(self):
        mesh = documents(ROOT / 'Malhas' / 'PainelUV.asset')[0]['Mesh']
        vertices = np.frombuffer(bytes.fromhex(mesh['m_VertexData']['_typelessdata']), '<f4').reshape(4, 8)
        self.assertTrue(np.all(vertices[:2, 7] == 0))
        self.assertTrue(np.all(vertices[2:, 7] == 1))
        index_hex = re.search(r'm_IndexBuffer: (\w+)', (ROOT / 'Malhas' / 'PainelUV.asset').read_text())[1]
        indices = struct.unpack('<6H', bytes.fromhex(index_hex))
        triangle = vertices[list(indices[:3]), :3]
        self.assertLess(np.cross(triangle[1] - triangle[0], triangle[2] - triangle[0])[2], 0)
        for item in json.loads((ROOT / 'posicionamento.json').read_text()):
            if item['prefab'] not in ['PlacaDerby', 'FaixaTorcida']:
                continue
            z = item['position'][2]
            normal_z = -math.cos(math.radians(item['yaw']))
            self.assertGreater(normal_z * (3909 - z), 0)

    def test_no_floating_flags_or_unresolved_local_references(self):
        scene = SCENE.read_text()
        self.assertEqual(scene.count('m_SourcePrefab:'), Path('Assets/DerbySegundoTempo/Cenas/Arquivo/Estadio_TPS.unity').read_text().count('m_SourcePrefab:') + 26)
        self.assertFalse((ROOT / 'Prefabs' / 'BandeiraTorcida.prefab').exists())
        for path in [SCENE, *(ROOT / 'Prefabs').glob('*.prefab'), *(ROOT / 'Materiais').glob('*.mat'), *(ROOT / 'Malhas').glob('*.asset')]:
            documents(path)
            text = path.read_text()
            ids = re.findall(r'^--- !u!\d+ &(-?\d+)', text, re.M)
            self.assertEqual(len(ids), len(set(ids)), str(path))
            for ref in re.findall(r'\{fileID: (-?\d+)\}', text):
                self.assertTrue(ref == '0' or ref in ids, (path, ref))

    def test_four_flags_follow_pitch_corners(self):
        placements = json.loads((ROOT / 'posicionamento.json').read_text())
        flags = [item['position'] for item in placements if item['prefab'] == 'BandeirinhaEscanteio']
        self.assertEqual(len(flags), 4)
        self.assertTrue(np.allclose(flags, pitch_corners(), atol=.001))
        mast = next(n['Transform'] for n in documents(ROOT / 'Prefabs/BandeirinhaEscanteio.prefab')
                    if 'Transform' in n and n['Transform']['m_LocalScale']['y'] == 1.8)
        self.assertAlmostEqual(mast['m_LocalPosition']['y'] - .9, 0)

    def test_station_text_area_is_clear(self):
        placements = json.loads((ROOT / 'posicionamento.json').read_text())
        self.assertFalse(any(item['prefab'].startswith('Posto') for item in placements))
        for station in ['EstacaoMedkit', 'EstacaoMunicao']:
            station_position = trans[station][:3, 3][[0, 2]]
            for item in placements:
                distance = np.linalg.norm(np.array(item['position'])[[0, 2]] - station_position)
                self.assertGreater(distance, 5, (station, item))

    def test_obstacles_block_physics_and_navigation_away_from_spawns(self):
        placements = json.loads((ROOT / 'posicionamento.json').read_text())
        barriers = [item for item in placements if item['prefab'].startswith('CoberturaCentral')]
        self.assertEqual(len(barriers), 10)
        for name in ['CoberturaCentralBaixa', 'CoberturaCentralAlta']:
            nodes = documents(ROOT / 'Prefabs' / (name + '.prefab'))
            collider = next(n['BoxCollider'] for n in nodes if 'BoxCollider' in n)
            navigation = next(n['NavMeshObstacle'] for n in nodes if 'NavMeshObstacle' in n)
            game_object = next(n['GameObject'] for n in nodes if 'GameObject' in n)
            self.assertEqual(game_object['m_Layer'], 6)
            self.assertEqual(collider['m_IsTrigger'], 0)
            self.assertEqual(collider['m_Enabled'], 1)
            self.assertEqual(navigation['m_Carve'], 1)
            self.assertEqual(navigation['m_Shape'], 1)
            self.assertTrue(np.allclose(list(navigation['m_Extents'].values()), np.array(list(collider['m_Size'].values())) / 2))
        for item in barriers:
            position = np.array(item['position'])[[0, 2]]
            for name, matrix in trans.items():
                if name.startswith('Ponto_Spawn'):
                    self.assertGreater(np.linalg.norm(matrix[:3, 3][[0, 2]] - position), 4, (item, name))

    def test_ball_has_physics_and_matching_round_visual(self):
        nodes = documents(ROOT / 'Prefabs/BolaFutebol.prefab')
        body = next(n['Rigidbody'] for n in nodes if 'Rigidbody' in n)
        sphere = next(n['SphereCollider'] for n in nodes if 'SphereCollider' in n)
        self.assertEqual(body['m_IsKinematic'], 0)
        self.assertEqual(body['m_UseGravity'], 1)
        self.assertEqual(body['m_CollisionDetection'], 2)
        self.assertEqual(sphere['m_IsTrigger'], 0)
        mesh = documents(ROOT / 'Malhas/BolaFutebol.asset')[0]['Mesh']
        data = bytes.fromhex(mesh['m_VertexData']['_typelessdata'])
        vertices = np.frombuffer(data, '<f4').reshape(-1, 8)[:, :3]
        self.assertTrue(np.allclose(np.linalg.norm(vertices, axis=1), sphere['m_Radius'], atol=.00001))
        self.assertEqual(len(mesh['m_SubMeshes']), 2)


if __name__ == '__main__':
    unittest.main()
