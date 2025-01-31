#version 330 core

uniform sampler2D texture0; // The image texture
uniform vec4 targetColor;   // The color to be replaced
uniform vec4 fillColor;     // The fill color
uniform float tolerance;    // The color tolerance
uniform int width;          // Width of the image
uniform int height;         // Height of the image

in vec2 fragTexCoord;      // Texture coordinate of the fragment
out vec4 fragColor;        // The output color

void main() {
    vec4 currentColor = texture(texture0, fragTexCoord);

    // Calculate the color difference (Euclidean distance in RGB space)
    float diff = distance(currentColor.rgb, targetColor.rgb);

    // If the color is within tolerance, apply the fill color
    if (diff <= tolerance) {
        fragColor = fillColor;
    } else {
        fragColor = currentColor;
    }
}
