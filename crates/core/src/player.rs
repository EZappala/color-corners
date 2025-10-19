use glam::{Vec3, Vec3Swizzles};

#[unsafe(no_mangle)]
pub extern "C" fn move_player(
    force_dir: Vec3,
    move_speed: f32,
    deceleration_speed: f32,
    current_v: Vec3,
    dt_fixed: f32,
) -> Vec3 {
    if force_dir != Vec3::ZERO {
        let v = force_dir.xyy().with_y(0.);
        v.lerp(current_v, move_speed * dt_fixed)
    } else {
        current_v.lerp(Vec3::ZERO, deceleration_speed * dt_fixed)
    }
}
